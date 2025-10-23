using Kalagaan.BlendShapesPresetTool;
using System.Collections;
using System.Linq;
using UnityEngine;

public class AgentEmotionController : MonoBehaviour
{
    private Vocalization vocalization;

    private readonly Emotion currentFacialEmotion = new();
    private readonly Emotion currentPhysiologicalEmotion = new();
    [SerializeField] private BlendShapesPresetController[] blendshapePresets;
    [SerializeField] private new Renderer renderer;
    [SerializeField] private Renderer eyesRenderer;
    [Range(0, 5)][SerializeField] float blendShapeFacialWeightCoef = 5f;
    [Range(0, 5)][SerializeField] float blendShapePhysiologicalWeightCoef = 5f;

    private Material faceMat, eyeLeftMat, eyeRightMat;
    public float durationInterpolation = 2f;

    private void Awake()
    {
        vocalization = GetComponent<Vocalization>();
        faceMat = renderer.materials.FirstOrDefault(mat => mat.name.Contains("Head"));
        if (eyesRenderer == null)
            eyesRenderer = renderer;
        eyeLeftMat = eyesRenderer.materials.FirstOrDefault(mat => mat.name.Contains("Cornea_L"));
        eyeRightMat = eyesRenderer.materials.FirstOrDefault(mat => mat.name.Contains("Cornea_R"));

    }

    public class Emotion
    {
        public float pos = 0;
        public float neg = 0;
        public float val = 0;
        public float surprise = 0;
    }
    public void SetEmotionFacialValenceAndSurprise(float positive, float negative, float surprise)
    {
        StopCoroutine("ChangeFacialEmotion");
        var emotion = new Emotion() { pos = positive, neg = negative, surprise = surprise, val = positive - negative };
        StartCoroutine(ChangeFacialEmotion(emotion));
    }

    IEnumerator ChangeFacialEmotion(Emotion emotion)
    {
        float elapsed = 0.0f;
        while (elapsed < durationInterpolation)
        {
            float lerpPositive = Mathf.Lerp(currentFacialEmotion.pos, emotion.pos, elapsed / durationInterpolation) ;
            float lerpNegative = Mathf.Lerp(currentFacialEmotion.neg, emotion.neg, elapsed / durationInterpolation);
            float lerpVal = lerpPositive - lerpNegative;
            
            //Debug.Log($"emotion facial: {lerpVal}");
            if (vocalization != null)
            {
                if (currentFacialEmotion.val <= 0.5f && lerpVal > 0.5f)
                    vocalization.PlayHappy();
                else if (currentFacialEmotion.val >= -0.5f & lerpVal < -0.5f)
                    vocalization.PlaySad();
            }
            lerpPositive = Mathf.Min(lerpPositive * blendShapeFacialWeightCoef, 1);
            lerpNegative = Mathf.Min(lerpNegative * blendShapeFacialWeightCoef, 1);
            lerpVal = lerpPositive - lerpNegative;

            float lerpSurprise = Mathf.Lerp(currentFacialEmotion.surprise, emotion.surprise, elapsed / durationInterpolation);

            for (int i = 0; i < blendshapePresets.Length; i++)
            {
                blendshapePresets[i].SetWeight(blendshapePresets[i].m_blendShapePreset.m_presets[0].name, lerpPositive);
                blendshapePresets[i].SetWeight(blendshapePresets[i].m_blendShapePreset.m_presets[1].name, lerpNegative);
                if (blendshapePresets[i].m_blendShapePreset.m_presets.Count > 2)
                    blendshapePresets[i].SetWeight(blendshapePresets[i].m_blendShapePreset.m_presets[2].name, lerpSurprise);
            }
            elapsed += Time.deltaTime;
            currentFacialEmotion.pos = lerpPositive;
            currentFacialEmotion.neg = lerpNegative;
            currentFacialEmotion.val = lerpVal;
            currentFacialEmotion.surprise = lerpSurprise;
            yield return null;
        }
    }

    public void SetEmotionPhysiologicalValenceAndSurprise(float positive, float negative, float surprise)
    {
        StopCoroutine("ChangePhysiologicalEmotion");
        var emotion = new Emotion() { pos = positive, neg = negative, surprise = surprise, val = positive - negative };
        StartCoroutine(ChangePhysiologicalEmotion(emotion));
    }

    IEnumerator ChangePhysiologicalEmotion(Emotion emotion)
    {
        float elapsed = 0.0f;
        while (elapsed < durationInterpolation)
        {
            float lerpPositive = Mathf.Lerp(currentPhysiologicalEmotion.pos, emotion.pos, elapsed / durationInterpolation) * blendShapePhysiologicalWeightCoef;
            float lerpNegative = Mathf.Lerp(currentPhysiologicalEmotion.neg, emotion.neg, elapsed / durationInterpolation) * blendShapePhysiologicalWeightCoef;
            lerpPositive = Mathf.Min(lerpPositive, 1);
            lerpNegative = Mathf.Min(lerpNegative, 1);
            float lerpVal = lerpPositive - lerpNegative;
            //Debug.Log($"emotion physiological: {lerpVal}");
            faceMat.SetFloat("_ColorBlendStrength", (lerpVal) / 4 + 0.25f);
            float valNorm = 1 - lerpPositive * 2 + 6 * lerpNegative;
            eyeLeftMat.SetFloat("_PupilScale", valNorm);
            eyeRightMat.SetFloat("_PupilScale", valNorm);
            faceMat.SetFloat("_MicroSmoothnessMod", lerpNegative * 1 / 5);
            elapsed += Time.deltaTime;
            currentPhysiologicalEmotion.pos = lerpPositive;
            currentPhysiologicalEmotion.neg = lerpNegative;
            currentPhysiologicalEmotion.val = lerpVal;
            yield return null;
        }
    }
}
