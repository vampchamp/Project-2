using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class HandDemoRecorder : MonoBehaviour
{
    [SerializeField] private string currentRecordingName = "handwash.demo";

    [Header("Live XR Hands")]
    public Transform leftRoot;
    public Transform rightRoot;

    [Header("Tutorial Hands")]
    public Transform tutorialLeftRoot;
    public Transform tutorialRightRoot;
    public GameObject tutorialHands;

    [Header("Recording")]
    [SerializeField] private int trimStartFrames = 30;
    [SerializeField] private int trimEndFrames = 15;

    public struct BoneFrame
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public class Frame
    {
        public BoneFrame[] left;
        public BoneFrame[] right;
    }

    public class Recording
    {
        public readonly List<Frame> frames = new();
    }

    private readonly List<Transform> leftBones = new();
    private readonly List<Transform> rightBones = new();
    private readonly List<Transform> tutorialLeftBones = new();
    private readonly List<Transform> tutorialRightBones = new();

    private readonly Dictionary<string, Recording> cache = new();

    private Recording activeRecording;
    private Recording captureBuffer;

    private bool preloaded;
    private string pendingPlayback;

    private bool isRecording;
    private bool isPlaying;
    private int playbackFrame;

    private IEnumerator Start()
    {
        GatherBones(leftRoot, leftBones);
        GatherBones(rightRoot, rightBones);
        GatherBones(tutorialLeftRoot, tutorialLeftBones);
        GatherBones(tutorialRightRoot, tutorialRightBones);

        if (tutorialHands != null)
            tutorialHands.SetActive(false);

        foreach (WashStep step in WashStepCatalog.Ordered)
            yield return LoadRecording(step + ".demo");

        preloaded = true;

        if (pendingPlayback != null)
        {
            string filename = pendingPlayback;
            pendingPlayback = null;
            StartPlayback(filename);
        }
    }

    private void LateUpdate()
    {
        if (isRecording)
            RecordFrame();

        if (isPlaying)
            PlayFrame();
    }

#if UNITY_EDITOR
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.rKey.wasPressedThisFrame) StartRecording();
        if (keyboard.sKey.wasPressedThisFrame) StopRecording();
        if (keyboard.pKey.wasPressedThisFrame) StartPlayback(currentRecordingName);
    }
#endif

    public void SetRecordingName(string filename) => currentRecordingName = filename;

    private static void GatherBones(Transform root, List<Transform> list)
    {
        if (root == null)
            return;

        list.Add(root);

        foreach (Transform child in root)
            GatherBones(child, list);
    }

    public void StartPlayback(string filename)
    {
        if (!preloaded)
        {
            pendingPlayback = filename;
            return;
        }

        if (!cache.TryGetValue(filename, out Recording recording) || recording == null || recording.frames.Count == 0)
        {
            Debug.LogWarning($"Recording '{filename}' is missing or empty.", this);
            return;
        }

        activeRecording = recording;
        playbackFrame = 0;
        isPlaying = true;

        if (tutorialHands != null)
            tutorialHands.SetActive(true);
    }

    public void StopPlayback()
    {
        pendingPlayback = null;
        isPlaying = false;

        if (tutorialHands != null)
            tutorialHands.SetActive(false);
    }

    private void PlayFrame()
    {
        if (activeRecording == null || playbackFrame >= activeRecording.frames.Count)
        {
            StopPlayback();
            return;
        }

        Frame frame = activeRecording.frames[playbackFrame];
        ApplyBones(tutorialLeftBones, frame.left);
        ApplyBones(tutorialRightBones, frame.right);

        playbackFrame++;
    }

    private static void ApplyBones(List<Transform> bones, BoneFrame[] frame)
    {
        int count = Mathf.Min(bones.Count, frame.Length);

        for (int i = 0; i < count; i++)
        {
            bones[i].localPosition = frame[i].position;
            bones[i].localRotation = frame[i].rotation;
        }
    }

    private IEnumerator LoadRecording(string filename)
    {
        if (cache.ContainsKey(filename))
            yield break;

        string path = Path.Combine(Application.streamingAssetsPath, filename);
        string url = path.Contains("://") ? path : "file://" + path;

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Could not load recording '{filename}': {request.error}", this);
            cache[filename] = null;
            yield break;
        }

        cache[filename] = ParseRecording(request.downloadHandler.data);
    }

    private static Recording ParseRecording(byte[] data)
    {
        var recording = new Recording();

        using var reader = new BinaryReader(new MemoryStream(data));

        int frameCount = reader.ReadInt32();

        for (int f = 0; f < frameCount; f++)
        {
            recording.frames.Add(new Frame
            {
                left = ReadBones(reader),
                right = ReadBones(reader)
            });
        }

        return recording;
    }

    private static BoneFrame[] ReadBones(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        var bones = new BoneFrame[count];

        for (int i = 0; i < count; i++)
        {
            bones[i].position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            bones[i].rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        return bones;
    }

    public void StartRecording()
    {
        Debug.Log("Recording started.", this);

        captureBuffer = new Recording();
        isPlaying = false;
        isRecording = true;
    }

    public void StopRecording()
    {
        if (!isRecording)
            return;

        isRecording = false;

        List<Frame> frames = captureBuffer.frames;

        int start = Mathf.Min(trimStartFrames, frames.Count);
        frames.RemoveRange(0, start);

        int end = Mathf.Min(trimEndFrames, frames.Count);
        frames.RemoveRange(frames.Count - end, end);

        SaveRecording(currentRecordingName);
        cache[currentRecordingName] = captureBuffer;
    }

    private void RecordFrame()
    {
        captureBuffer.frames.Add(new Frame
        {
            left = CaptureBones(leftBones),
            right = CaptureBones(rightBones)
        });
    }

    private static BoneFrame[] CaptureBones(List<Transform> bones)
    {
        var result = new BoneFrame[bones.Count];

        for (int i = 0; i < bones.Count; i++)
        {
            result[i].position = bones[i].localPosition;
            result[i].rotation = bones[i].localRotation;
        }

        return result;
    }

    public void SaveRecording(string filename)
    {
        string path = Path.Combine(Application.streamingAssetsPath, filename);
        Directory.CreateDirectory(Application.streamingAssetsPath);

        using (var writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(captureBuffer.frames.Count);

            foreach (Frame frame in captureBuffer.frames)
            {
                WriteBones(writer, frame.left);
                WriteBones(writer, frame.right);
            }
        }

        Debug.Log($"Saved recording to:\n{path}", this);
    }

    private static void WriteBones(BinaryWriter writer, BoneFrame[] bones)
    {
        writer.Write(bones.Length);

        foreach (BoneFrame bone in bones)
        {
            writer.Write(bone.position.x);
            writer.Write(bone.position.y);
            writer.Write(bone.position.z);
            writer.Write(bone.rotation.x);
            writer.Write(bone.rotation.y);
            writer.Write(bone.rotation.z);
            writer.Write(bone.rotation.w);
        }
    }
}
