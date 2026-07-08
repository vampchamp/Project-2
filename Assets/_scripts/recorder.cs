using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

public class recorder : MonoBehaviour
{
    [SerializeField]
    private string currentRecordingName = "handwash.demo";
    
    [Header("Live XR Hands")]
    public Transform leftRoot;
    public Transform rightRoot;

    [Header("Tutorial Hands")]
    public Transform tutorialLeftRoot;
    public Transform tutorialRightRoot;
    public GameObject tutorialHands;

    [System.Serializable]
    public class BoneFrame
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class Frame
    {
        public List<BoneFrame> left = new();
        public List<BoneFrame> right = new();
    }

    [System.Serializable]
    public class Recording
    {
        public List<Frame> frames = new();
    }
    [SerializeField] private int trimStartFrames = 30;
    [SerializeField] private int trimEndFrames = 15;

    private readonly List<Transform> leftBones = new();
    private readonly List<Transform> rightBones = new();

    private readonly List<Transform> tutorialLeftBones = new();
    private readonly List<Transform> tutorialRightBones = new();

    private Recording recording = new();

    private bool isRecording;
    private bool isPlaying;
    private int playbackFrame;

    public void SetRecordingName(string filename)
    {
        currentRecordingName = filename;
    }
    
    void Start()
    {
        GatherBones(leftRoot, leftBones);
        GatherBones(rightRoot, rightBones);

        GatherBones(tutorialLeftRoot, tutorialLeftBones);
        GatherBones(tutorialRightRoot, tutorialRightBones);

        tutorialHands.SetActive(false);

        Debug.Log($"Live Left Bones: {leftBones.Count}");
        Debug.Log($"Tutorial Left Bones: {tutorialLeftBones.Count}");
    }

    void LateUpdate()
    {
        if (isRecording)
            RecordFrame();

        if (isPlaying)
            PlayFrame();
    }
    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
            StartRecording();

        if (Keyboard.current.sKey.wasPressedThisFrame)
            StopRecording();

        if (Keyboard.current.pKey.wasPressedThisFrame)
            StartPlayback(currentRecordingName);
    }

    void GatherBones(Transform root, List<Transform> list)
    {
        list.Add(root);

        foreach (Transform child in root)
            GatherBones(child, list);
    }

    public void StartRecording()
    {
        Debug.Log("Recording Started");

        recording.frames.Clear();
        playbackFrame = 0;
        isPlaying = false;
        isRecording = true;
    }

    public void StopRecording()
    {
        isRecording = false;
        
        int start = Mathf.Min(trimStartFrames, recording.frames.Count);
        recording.frames.RemoveRange(0, start);
        
        int end = Mathf.Min(trimEndFrames, recording.frames.Count);
        recording.frames.RemoveRange(recording.frames.Count - end, end);

        SaveRecording(currentRecordingName);
    }

    public void StartPlayback(string filename)
    {
        LoadRecording(filename);

        if (recording.frames.Count == 0)
        {
            Debug.LogWarning($"Recording '{filename}' not found.");
            return;
        }

        tutorialHands.SetActive(true);

        playbackFrame = 0;
        isPlaying = true;
    }
    public void StopPlayback()
    {
        isPlaying = false;
        tutorialHands.SetActive(false);
    }

    void RecordFrame()
    {
        Frame frame = new();

        foreach (Transform bone in leftBones)
        {
            frame.left.Add(new BoneFrame
            {
                position = bone.localPosition,
                rotation = bone.localRotation
            });
        }

        foreach (Transform bone in rightBones)
        {
            frame.right.Add(new BoneFrame
            {
                position = bone.localPosition,
                rotation = bone.localRotation
            });
        }

        recording.frames.Add(frame);
    }

    void PlayFrame()
    {
        if (playbackFrame >= recording.frames.Count)
        {
            Debug.Log("Playback Finished");

            isPlaying = false;
            tutorialHands.SetActive(false);
            return;
        }

        Frame frame = recording.frames[playbackFrame];

        for (int i = 0; i < tutorialLeftBones.Count; i++)
        {
            tutorialLeftBones[i].localPosition = frame.left[i].position;
            tutorialLeftBones[i].localRotation = frame.left[i].rotation;
        }

        for (int i = 0; i < tutorialRightBones.Count; i++)
        {
            tutorialRightBones[i].localPosition = frame.right[i].position;
            tutorialRightBones[i].localRotation = frame.right[i].rotation;
        }

        playbackFrame++;
    }
    public void SaveRecording(string filename)
    {
        string path = Path.Combine(Application.streamingAssetsPath, filename);

        Directory.CreateDirectory(Application.streamingAssetsPath);

        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(recording.frames.Count);

            foreach (Frame frame in recording.frames)
            {
                // LEFT HAND
                writer.Write(frame.left.Count);

                foreach (BoneFrame bone in frame.left)
                {
                    writer.Write(bone.position.x);
                    writer.Write(bone.position.y);
                    writer.Write(bone.position.z);

                    writer.Write(bone.rotation.x);
                    writer.Write(bone.rotation.y);
                    writer.Write(bone.rotation.z);
                    writer.Write(bone.rotation.w);
                }

                // RIGHT HAND
                writer.Write(frame.right.Count);

                foreach (BoneFrame bone in frame.right)
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

        Debug.Log($"Saved recording to:\n{path}");
    }
    public void LoadRecording(string filename)
    {
        string path = Path.Combine(Application.streamingAssetsPath, filename);

        if (!File.Exists(path))
        {
            Debug.LogWarning("Recording file not found.");
            recording.frames.Clear();
            return;
        }

        recording.frames.Clear();

        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            int frameCount = reader.ReadInt32();

            for (int f = 0; f < frameCount; f++)
            {
                Frame frame = new Frame();

                // LEFT HAND
                int leftCount = reader.ReadInt32();

                for (int i = 0; i < leftCount; i++)
                {
                    BoneFrame bone = new BoneFrame();

                    bone.position = new Vector3(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle());

                    bone.rotation = new Quaternion(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle());

                    frame.left.Add(bone);
                }

                // RIGHT HAND
                int rightCount = reader.ReadInt32();

                for (int i = 0; i < rightCount; i++)
                {
                    BoneFrame bone = new BoneFrame();

                    bone.position = new Vector3(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle());

                    bone.rotation = new Quaternion(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle());

                    frame.right.Add(bone);
                }

                recording.frames.Add(frame);
            }
        }

        Debug.Log($"Loaded {recording.frames.Count} frames.");
    }
}