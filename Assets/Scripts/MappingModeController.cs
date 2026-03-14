using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;
using TMPro;

public class MappingModeController : MonoBehaviour
{
    public TMP_Text positionLabel;
    public TMP_Text timerLabel;
    public TMP_Text songLengthLabel;
    public TMP_Text playLabel;
    public TMP_Text songFileLabel;
    public InputField speedInput;
    public AudioSource audio;
    public AudioClip songClip;
    public float timerCurrent;
    public float songLength;
    public String timerString;
    public String songString;
    public String songFilePath;
    private TimeSpan _timerTimeSpan;
    private TimeSpan _songTimeSpan;

    public GameObject buttonPrefab;
    public GameObject panelMapper;
    public GameObject panelNaming;
    public Slider sliderTimer;
    public float scaleSensitivity;
    
    public KeyCode additionalKey = KeyCode.Q;

    private float curTime;
    private float[] curPosition = new float[2];
    private MappingButton curButton;
    private Vector3 lastPosition = new Vector3();

    private SortedDictionary<float, ButtonItem> mappedButtons = new SortedDictionary<float, ButtonItem>();
    private SortedList<float, MappingButton> displayingButtons = new SortedList<float, MappingButton>();
    
    public NamingPanel namingPanel;

    private void Start()
    {
        SetupAudioClip();
        
        //output song's length
    }

    private void Awake()
    {
        speedInput.text = "1";
    }

    private void Update()
    {
        UpdatePositionLabel(Input.mousePosition.x, Input.mousePosition.y);
        if (audio.isPlaying)
        {
            UpdateSlider();
            UpdateTimerLabel();
        }
        
        if (audio.isPlaying && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(additionalKey)))
        {
            curPosition[0] = Input.mousePosition.x;
            curPosition[1] = Input.mousePosition.y;
            curTime = audio.time * 1000f;

            bool isSlider = true;
            curButton = CreateButton(curPosition[0], curPosition[1], isSlider);
            lastPosition = Input.mousePosition;
        }

        //doesnt let you place notes if hovering over UI
        if (!(curPosition[1] < 420) && !(curPosition[1] > 70)) return;
        
        if ((Input.GetMouseButton(0) || Input.GetKey(additionalKey)) && curButton != null)
        {
            Vector3 diffPos = Input.mousePosition - lastPosition;
            curButton.dragRegion.transform.localScale += new Vector3(((Mathf.Abs(diffPos.x) + Mathf.Abs(diffPos.y)) * 1 / 60f * scaleSensitivity), 0f, 0f);
            curButton.dragRegion.transform.position += (diffPos * (scaleSensitivity * 0.75f));

            curButton.InitializeLastButton(Input.mousePosition.x, Input.mousePosition.y);
            lastPosition = Input.mousePosition;
        }
        else if ((Input.GetMouseButtonUp(0) || Input.GetKeyUp(additionalKey)) && curButton)
        {
            ButtonItem button = new ButtonItem
            {
                time = curTime + GetSpeedInputVal(),
                position = new float[] { curPosition[0], curPosition[1] },
                endPosition = new float[] { lastPosition.x, lastPosition.y },
                isDrag = curButton.isDrag
            };

            mappedButtons.Add(button.time, button);
            curButton = null;
            lastPosition = Vector3.zero;
        }

        if (curButton && curButton.buttonTimer.ElapsedMilliseconds > curButton.duration)
        {
            ButtonItem button = new ButtonItem
            {
                time = curTime + GetSpeedInputVal(),
                position = new float[] { curPosition[0], curPosition[1] },
                endPosition = new float[] { lastPosition.x, lastPosition.y },
                isDrag = curButton.isDrag
            };

            mappedButtons.Add(button.time, button);
            curButton.DestroyButton();
            curButton = null;
            lastPosition = Vector3.zero;
        }
    }

    private MappingButton CreateButton(float x, float y, bool isSlider = false)
    {
        GameObject button = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity);
        button.transform.SetParent(this.transform, false);

        MappingButton mappingButton = button.GetComponent<MappingButton>();
        mappingButton.duration = GetSpeedInputVal();
        mappingButton.isDrag = isSlider;
        mappingButton.SetType(isSlider);
        mappingButton.InitializeFirstButton(x, y);

        displayingButtons.Add(audio.time * 1000f, mappingButton);
        return mappingButton;
    }

    private void UpdatePositionLabel(float x, float y)
    {
        positionLabel.text = $"x: {x}, y: {y}";
    }

    private void UpdateTimerLabel()
    {
        // // song's current Time
        // _timerTimeSpan = System.TimeSpan.FromMilliseconds(mapTimer.ElapsedMilliseconds);
        // timerString = $"{_timerTimeSpan.Minutes:D2}:{_timerTimeSpan.Seconds:D2}:{_timerTimeSpan.Milliseconds:D3}";
        // timerLabel.text = timerString;
        timerString = audio.time.ToString("F2");
        timerLabel.text = timerString;
    }

    public void SliderSetTime()
    {
        audio.time = sliderTimer.value;
        timerString = sliderTimer.value.ToString("F2");
        timerLabel.text = timerString;
    }

    public void UpdateSlider()
    {
        // sliderTimer.value = mapTimer.ElapsedMilliseconds / 1000;
        sliderTimer.value = audio.time;
    }

    public void OnPlayButtonPress()
    {
        if (audio.isPlaying)
        {
            audio?.Pause();
            // mapTimer.Stop();
            playLabel.text = "Start";
        }
        else
        {
            audio?.Play();
            if (audio != null) audio.time = sliderTimer.value;
            speedInput.gameObject.SetActive(false);
            // mapTimer.Start();
            playLabel.text = "Stop";
        }
    }

    public void RestartMapping()
    {
        if (audio.isPlaying)
            audio.Stop();
        audio.time = 0;

        // mapTimer.Reset();
        playLabel.text = "Start";
        speedInput.gameObject.SetActive(true);

        foreach (var kvp in displayingButtons)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }

        displayingButtons.Clear();
        mappedButtons.Clear();
        curButton = null;
        lastPosition = Vector3.zero;
        timerLabel.text = "00:00:00";
        positionLabel.text = "x: 0, y: 0";

        UnityEngine.Debug.Log("🔁 Mapping di-reset ulang.");
    }

    public void OnFinishMapping()
    {
        ButtonData buttonData = new ButtonData();
        foreach (var pair in mappedButtons)
        {
            buttonData.buttons.Add(pair.Value);
        }

        GameData.TempMapping = buttonData.buttons;
        UnityEngine.Debug.Log("✅ Mapping disimpan ke memory.");

        panelMapper.SetActive(false);
        panelNaming.SetActive(true);
    }

    public void Saving()
    {
        ButtonData buttonData = new ButtonData();
        foreach (var pair in mappedButtons)
        {
            buttonData.buttons.Add(pair.Value);
        }
        GameData.TempMapping = buttonData.buttons;
        Debug.Log("✅ Mapping disimpan ke memory.");
    }

    private float GetSpeedInputVal()
    {
        float speed = 1f;
        float.TryParse(speedInput.text, out speed);
        return speed * 1000;
    }

    private void SetupAudioClip()
    {
        string defaultFolder = Application.streamingAssetsPath;
        var extensions = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Audio File", defaultFolder, extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            StartCoroutine(LoadAudioFromFile(paths[0]));
        }
        else
        {
            Debug.LogWarning("❌ Tidak ada file audio dipilih.");
        }
    }

    public void RestartConfirm()
    {
        audio.Pause();
        playLabel.text = "Start";
    }

    public void SaveAudio()
    {
        namingPanel.audioSrcPath = songFilePath;

    }
    private IEnumerator LoadAudioFromFile(string path)
    {
        string url = "file://" + path;
        using (WWW www = new WWW(url))
        {
            yield return www;

            AudioClip clip = www.GetAudioClip(false, false);
            if (clip)
            {
                //load audio into AudioClip
                audio.clip = clip;
                songClip = clip;
                
                //set the length
                sliderTimer.maxValue = clip.length;
                songString = Mathf.Round(clip.length - clip.length % 60) / 60 + ":" +
                             Mathf.Round(clip.length % 60);
                songLengthLabel.text = songString;
                
                //set title
                songFilePath = Path.GetFileName(path);
                songFileLabel.text = songFilePath;
                
                Debug.Log("✅ AudioClip berhasil dimuat: " + Path.GetFileName(path));
                Debug.Log("Song Length: " + clip.length + " seconds. (" + Mathf.Round(clip.length - clip.length%60) / 60 + ":" + Mathf.Round(clip.length%60) + ")");
            }
            else
            {
                Debug.LogError("❌ Gagal memuat AudioClip dari: " + path);
            }
        }
    }
}
