using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FeverWaveText : MonoBehaviour
{
    [Header("텍스트 설정")]
    [SerializeField] private TMP_Text targetText;

    [Header("파도 효과")]
    [SerializeField] private float waveHeight = 10f;

    [Tooltip("파도가 움직이는 속도")]
    [SerializeField] private float waveSpeed = 5f;

    [Tooltip("글자 사이의 파도 시간 차이")]
    [SerializeField] private float waveOffset = 0.5f;

    [Header("자동 실행")]
    [SerializeField] private bool playOnEnable = true;

    private float elapsedTime;
    private bool isPlaying;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
        
       
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    public void Play()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        targetText.text = "Fever Time!";
        targetText.maxVisibleCharacters = int.MaxValue;

        elapsedTime = 0f;
        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;

        if (targetText != null)
        {
            targetText.ForceMeshUpdate();
        }
    }

    private void LateUpdate()
    {
        if (!isPlaying || targetText == null)
            return;

        elapsedTime += Time.unscaledDeltaTime;

        // 원래 글자 위치로 초기화
        targetText.ForceMeshUpdate();

        TMP_TextInfo textInfo = targetText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            // 글자마다 위아래로 움직이는 파도 효과
            float offsetY = Mathf.Sin(
                elapsedTime * waveSpeed - i * waveOffset
            ) * waveHeight;

            int materialIndex = character.materialReferenceIndex;
            int vertexIndex = character.vertexIndex;

            Vector3[] vertices =
                textInfo.meshInfo[materialIndex].vertices;

            Vector3 offset = new Vector3(0f, offsetY, 0f);

            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] += offset;
            }
        }

        targetText.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Vertices
        );
    }
}