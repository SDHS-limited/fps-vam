using System.Collections.Generic;
using UnityEngine;

public class KeyManager : MonoBehaviour
{
    public static KeyManager instance; // 싱글톤: 어디서든 접근 가능하게 만듦
    
    // 액션 이름과 설정된 키를 연결해주는 딕셔너리
    public Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();

    void Awake()
    {
        // 씬이 넘어가도 파괴되지 않게 설정
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        LoadKeys();
    }

    // 저장된 키를 불러오는 함수 (저장된 게 없으면 기본값 세팅)
    public void LoadKeys()
    {
        keys.Clear();
        keys.Add("Forward", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Forward", "W")));
        keys.Add("Backward", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Backward", "S")));
        keys.Add("Left", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Left", "A")));
        keys.Add("Right", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Right", "D")));
        keys.Add("Jump", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Jump", "Space")));
        keys.Add("Attack", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Attack", "Mouse0"))); // Mouse0 = 좌클릭
        keys.Add("Push", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Push", "Mouse1")));    // Mouse1 = 우클릭
        keys.Add("Dash", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Dash", "LeftShift")));
        keys.Add("Grenade", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Grenade", "G")));
    }

    // 새로운 키를 저장하는 함수
    public void SaveKey(string action, KeyCode keyCode)
    {
        keys[action] = keyCode;
        PlayerPrefs.SetString(action, keyCode.ToString());
        PlayerPrefs.Save();
    }
}
