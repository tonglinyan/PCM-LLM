using System;

namespace Core
{
    [Serializable]
    public class PCMEmotionFacialParameters
    {
        public class OnEmotionFacialParametersChangedArgs : EventArgs
        {
            public float NewValence { get; set; }
            public float NewSurpriseLevel { get; set; }
            public UnityEngine.GameObject GameObject { get; set; }
            public OnEmotionFacialParametersChangedArgs(float newValence, float newSurpriseLevel, UnityEngine.GameObject gameObject)
            {
                NewValence = newValence;
                NewSurpriseLevel = newSurpriseLevel;
                GameObject = gameObject;
            }
        }

        private float m_positiveLevel = 0;
        private float m_negativeLevel = 0;
        private float m_surpriseLevel = 0f;

        // protected by compilator
        public event EventHandler<OnEmotionFacialParametersChangedArgs> OnEmotionFacialParametersChanged = delegate { };

        private UnityEngine.GameObject m_gameObject;

        public PCMEmotionFacialParameters(UnityEngine.GameObject gameObject)
        {
            m_gameObject = gameObject;
        }

        public void SetEmotionFacialParameters(float positiveLevel, float negativeLevel, float surpriseLevel)
        {
            if ((positiveLevel != m_positiveLevel) || (negativeLevel != m_negativeLevel) || (surpriseLevel != m_surpriseLevel))
            {
                m_positiveLevel = positiveLevel;
                m_negativeLevel = negativeLevel;
                m_surpriseLevel = surpriseLevel;
                OnEmotionFacialParametersChanged(this, new OnEmotionFacialParametersChangedArgs(Valence, m_surpriseLevel, m_gameObject));
            }
        }

        public float Valence
        {
            get { return m_positiveLevel - m_negativeLevel; }
        }

        public float PositiveLevel
        {
            get { return m_positiveLevel; }
        }

        public float NegativeLevel
        {
            get { return m_negativeLevel; }
        }

        public float SurpriseLevel
        {
            get { return m_surpriseLevel; }
        }

        public Interfacing.Emotion ToPCMEmotion()
        {
            Interfacing.Emotion emotion = new()
            {
                Positive = m_positiveLevel,
                Negative = m_negativeLevel,
                Valence = Valence,
                Surprise = m_surpriseLevel
            };
            return emotion;
        }
    }
}
