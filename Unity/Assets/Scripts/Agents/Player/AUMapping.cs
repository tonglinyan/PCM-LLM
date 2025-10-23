using System.Collections.Generic;
using UnityEngine;
using System;
using SensorDataStructure;
using Core;
using Unity.VisualScripting;
using System.CodeDom;
using System.Linq.Expressions;

namespace AUMapping
{

    public class AUToEmotion : MonoBehaviour
    {
        private static int nbEmotion = 6;
        private static int nbAU = 47;
        //public TextMeshProUGUI textMeshpro; 
        private Dictionary<string, float> cumulativeTime = new Dictionary<string, float>{
            {"positive", 0},
            {"negative", 0},
            {"surprise", 0},
        }; 

        private Dictionary<string, double> valenceMax = new Dictionary<string, double>{
            {"positive", 0},
            {"negative", 0},
            {"surprise", 0},
        };   

        private static Assets.Scripts.Matrix mappingMatrix = new Assets.Scripts.Matrix(nbEmotion, nbAU);

        private static double lambda = 5f;//1.3f;
        private readonly Dictionary<string, List<string>> mappingEmoAU = new Dictionary<string, List<string>>
        {
            { "Happiness", new List<string> { "6", "12" } },
            { "Sadness", new List<string> { "1", "4", "15" } },
            { "Surprise", new List<string> { "1", "2", "5B", "26" } },
            { "Fear", new List<string> { "1", "2", "4", "5", "7", "20", "26" } },
            { "Anger", new List<string> { "4", "5", "7", "23" } },
            { "Disgust", new List<string> { "9", "15", "17" } }
        };

        private List<ActionUnit> actionUnits = new List<ActionUnit>
        {
            new ActionUnit(0, "Neutral face"),
            new ActionUnit(1, "Inner brow raiser"),
            new ActionUnit(2, "Outer brow raiser"),
            new ActionUnit(4, "Brow lowerer"),
            new ActionUnit(5, "Upper lid raiser"),
            new ActionUnit(6, "Cheek raiser"),
            new ActionUnit(7, "Lid tightener"),
            new ActionUnit(8, "Lips toward each other"),
            new ActionUnit(9, "Nose wrinkler"),
            new ActionUnit(10, "Upper lip raiser"),
            new ActionUnit(11, "Nasolabial deepener"),
            new ActionUnit(12, "Lip corner puller"),
            new ActionUnit(13, "Sharp lip puller"),
            new ActionUnit(14, "Dimpler"),
            new ActionUnit(15, "Lip corner depressor"),
            new ActionUnit(16, "Lower lip depressor"),
            new ActionUnit(17, "Chin raiser"),
            new ActionUnit(18, "Lip pucker"),
            new ActionUnit(19, "Tongue show"),
            new ActionUnit(20, "Lip stretcher"),
            new ActionUnit(21, "Neck tightener"),
            new ActionUnit(22, "Lip funneler"),
            new ActionUnit(23, "Lip tightener"),
            new ActionUnit(24, "Lip pressor"),
            new ActionUnit(25, "Lips part"),
            new ActionUnit(26, "Jaw drop"),
            new ActionUnit(27, "Mouth stretch"),
            new ActionUnit(28, "Lip suck"),
            new ActionUnit(41, "Lid droop"),
            new ActionUnit(42, "Slit"),
            new ActionUnit(43, "Eyes Closed"),
            new ActionUnit(44, "Squint"),
            new ActionUnit(45, "Blink"),
            new ActionUnit(46, "Wink")
        };

        private Assets.Scripts.Matrix emotionWeights = new Assets.Scripts.Matrix(nbEmotion, 1, new double[] { 1, -1, 1, -1, -1, -1 });

        private void Awake()
        {
            int row = 0;
            foreach (var emotion in mappingEmoAU)
            {
                var AUList = emotion.Value;
                foreach (string au in AUList)
                {
                    double prob = 1;
                    string au_new = au;
                    if (au.Contains("A") || au.Contains("B"))
                    {
                        if (au.Contains("A"))
                        {
                            prob = 0.2;
                            au_new = au.Remove(au.IndexOf("A"), 1);
                        }
                        if (au.Contains("B"))
                        {
                            prob = 0.4;
                            au_new = au.Remove(au.IndexOf("B"), 1);
                        }
                    }
                    int column = int.Parse(au_new);
                    //Debug.Log($"{row} {column}");
                    mappingMatrix[row, column] = prob;
                }
                row++;
            }
        }

        public double GetValenceValue(FaceData faceData, Interfacing.EmotionSystem emotions)
        {
            Debug.Log(faceData.IsAvailable);
            if (faceData.IsAvailable)
            {
                double[] weights = GetWeight(faceData);
                //double[] confidence = GetConfidence(faceData);

                Assets.Scripts.Matrix vectorWeight = new Assets.Scripts.Matrix(nbAU, 1, weights);
                //Assets.Scripts.Matrix.PrintMatrix(mappingMatrix);
                //Assets.Scripts.Matrix.PrintMatrix(vectorWeight);
                //Assets.Scripts.Matrix confidenceM = new Assets.Scripts.Matrix(nbAU, 1, confidence);

                //Assets.Scripts.Matrix weight = Assets.Scripts.Matrix.PointwisedMultiply(weightM, confidenceM);
                //Assets.Scripts.Matrix correlationEmo = Assets.Scripts.Matrix.Normalization(Assets.Scripts.Matrix.Multiply(mappingMatrix, vectorWeight));
                //Assets.Scripts.Matrix.PrintMatrix(correlationEmo);

                /*Assets.Scripts.Matrix weightedCorrelationEmo = Assets.Scripts.Matrix.PointwisedMultiply(correlationEmo, emotionWeights);
                Assets.Scripts.Matrix.PrintMatrix(weightedCorrelationEmo);

                emotions.facial.Positive = 2 / (1 + Math.Exp(-lambda * weightedCorrelationEmo[0, 0])) - 1;
                emotions.facial.Negative = 2 / (1 + Math.Exp(-lambda * 0.25 * (weightedCorrelationEmo[1, 0] + weightedCorrelationEmo[3, 0] + weightedCorrelationEmo[4, 0] + weightedCorrelationEmo[5, 0]))) - 1;
                emotions.facial.Surprise = 2 / (1 + Math.Exp(-lambda * weightedCorrelationEmo[2, 0])) - 1;
                emotions.facial.Valence = emotions.facial.Positive - emotions.facial.Negative;*/

                Assets.Scripts.Matrix correlationEmo = Assets.Scripts.Matrix.Multiply(mappingMatrix, vectorWeight);
                //Assets.Scripts.Matrix.PrintMatrix(correlationEmo);
                correlationEmo = WeightedCorrelation(correlationEmo);
                //Assets.Scripts.Matrix.PrintMatrix(correlationEmo);
                var pos = 2 / (1 + Math.Exp(-lambda * correlationEmo[0, 0])) - 1;
                var neg = 2 / (1 + Math.Exp(-lambda * (correlationEmo[1, 0] + correlationEmo[3, 0] + correlationEmo[4, 0] + correlationEmo[5, 0]) / 4)) - 1;
                var surp = 2 / (1 + Math.Exp(-lambda * correlationEmo[2, 0])) - 1;

                emotions.facial.Positive = Update(pos, "positive");
                emotions.facial.Negative = Update(neg, "negative");
                emotions.facial.Surprise = Update(surp, "surprise");
                
                emotions.facial.Valence = emotions.facial.Positive - emotions.facial.Negative;

                double Update(double valence, string key) 
                {
                    double res;
                    var dissipationValence = valenceMax[key] * MathF.Exp(-cumulativeTime[key]);
                    if (valence > dissipationValence) {
                        res = valence;
                        valenceMax[key] = valence;
                        cumulativeTime[key] = Time.fixedDeltaTime;
                    }
                    else{
                        cumulativeTime[key] += Time.fixedDeltaTime;
                        res = dissipationValence;
                    }
                    return res;
                }

            }
            /*emotions.facial.Positive = 0.9;
            emotions.facial.Negative = 0.0;
            emotions.facial.Valence = emotions.facial.Positive - emotions.facial.Negative;*/

            emotions.physiological.Positive = emotions.facial.Positive;
            emotions.physiological.Negative = emotions.facial.Positive;
            emotions.physiological.Valence = emotions.facial.Valence;


            //Debug.Log($"valence positive {emotions.facial.Positive}");
            //Debug.Log($"valence negative {emotions.facial.Negative}");
            //Debug.Log($"valence surprise {emotions.facial.Surprise}");
            Debug.Log($"valence {emotions.facial.Valence}");

            return emotions.facial.Valence;
        }

        private Assets.Scripts.Matrix WeightedCorrelation(Assets.Scripts.Matrix matrix)
        {
            int i = 0;
            foreach (var mapping in mappingEmoAU)
            {
                matrix[i, 0] = matrix[i, 0] / mapping.Value.Count;
                i++;
            }
            return matrix;
        }

        public double[] GetWeight(FaceData faceData)
        {
            ActivationData[] actionUnit = faceData.ActionUnitData;
            double[] weights = new double[nbAU];

            //for (int i = 0; i < nbAU; i++)
            foreach (var au in actionUnits)
            {
                int i = au.Number;
                string label = au.Label;

                List<ActivationData> matchedActivationDataList = new List<ActivationData>();
                foreach (var activation in actionUnit)
                {
                    if (activation.Label.Contains(label))
                    {
                        matchedActivationDataList.Add(activation);
                        //Debug.Log($"{label} {activation.Label} {matchedActivationDataList.Count}");
                    }
                    //Debug.Log($"{label} {activation.Label}");
                }

                if (matchedActivationDataList.Count > 0)
                {
                    double max = 0;
                    foreach (ActivationData activation in matchedActivationDataList)
                    {
                        max = (activation.Weight > max) ? activation.Weight : max;
                    }
                    weights[i] = max;
                    //Debug.Log($"{label} {max}");
                }
                else
                {
                    weights[i] = 0f;
                }

            }

            return weights;
        }

        /*public double[] GetConfidence(FaceData faceData)
        {
            FaceConfidenceZoneData[] confidenceZone = faceData.FaceConfidenceData;
            double[] confidences = new double[nbAU];

            for (int i = 0; i < nbAU; i++)
            {
                string label = actionUnits[i].Label;

                List<FaceConfidenceZoneData> matchedConfidenceDataList = new List<FaceConfidenceZoneData>();

                foreach (var confidence in confidenceZone)
                {
                    if (confidence.Label.Contains(label) && confidence.IsValid)
                    {
                        matchedConfidenceDataList.Add(confidence);
                    }
                }

                if (matchedConfidenceDataList.Count > 0)
                {
                    double max = 0;
                    foreach (FaceConfidenceZoneData confidenceData in matchedConfidenceDataList)
                    {
                        max = (confidenceData.Confidence > max)? confidenceData.Confidence : max;
                    }
                    confidences[i] = max;
                }
                else
                {
                    confidences[i] = 0f;
                }
            }

            return confidences;
        }*/
    }
}
