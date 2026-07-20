using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio; // ★ 오디오 믹서를 사용하기 위해 추가

public class SettingsMenu : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    public TMP_Dropdown resolutionDropdown;
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityText;

    [Header("오디오 설정")]
    public AudioMixer audioMixer; // ★ 아까 만든 MainMixer를 연결할 변수
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Slider voiceSlider;

    private Resolution[] resolutions;

    void OnEnable()
    {
        // ── 1. 해상도 설정 초기화 ──
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // ── 2. 감도 설정 초기화 ──
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 100f);
        sensitivitySlider.value = savedSensitivity;
        UpdateSensitivityText(savedSensitivity);

        // ── 3. 오디오 설정 초기화 및 적용 ──
        // 저장된 볼륨이 없으면 기본값 1(최대치)로 설정
        float savedBGM = PlayerPrefs.GetFloat("BGM_Vol", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFX_Vol", 1f);
        float savedVoice = PlayerPrefs.GetFloat("Voice_Vol", 1f);

        bgmSlider.value = savedBGM;
        sfxSlider.value = savedSFX;
        voiceSlider.value = savedVoice;

        SetBGMVolume(savedBGM);
        SetSFXVolume(savedSFX);
        SetVoiceVolume(savedVoice);
    }

    // ── 기존 마우스 & 해상도 함수 ──
    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
        UpdateSensitivityText(sensitivity);

        camera camScript = FindObjectOfType<camera>();
        if (camScript != null)
        {
            camScript.UpdateSensitivity(sensitivity);
        }
    }

    private void UpdateSensitivityText(float value)
    {
        if (sensitivityText != null)
        {
            sensitivityText.text = Mathf.RoundToInt(value).ToString();
        }
    }

    // ── ★ 오디오 조절 함수 ──
    // 슬라이더 값을 데시벨(dB)로 변환하여 믹서에 적용하고 저장합니다.
    public void SetBGMVolume(float volume)
    {
        audioMixer.SetFloat("BGM", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("BGM_Vol", volume);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFX", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFX_Vol", volume);
    }

    public void SetVoiceVolume(float volume)
    {
        audioMixer.SetFloat("Voice", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("Voice_Vol", volume);
    }
}