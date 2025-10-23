using System;
using System.Globalization;
using System.Numerics;

namespace PCM.Verbal.LLM
{
    public class LLMOutput
    {
        public Dictionary<string, string> preference;
        public Emotions emotion;
        public Move move;

        public bool CleanPreference(string input, out double result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim();


            bool isPercent = input.EndsWith("%");
            if (isPercent)
            {
                input = input.TrimEnd('%');
                if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double percentValue))
                {
                    result = percentValue + 100 / 200.0;
                    return true;
                }
            }
            else
            {
                if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double decimalValue))
                {
                    result = (decimalValue > 1 || decimalValue < -1) ? (decimalValue + 100) / 200 : (decimalValue + 1) / 2;
                    return true;
                }
            }

            return false;
        }
    
    }

    public class Move
        {
            public string action;
            public string direction;
        }

    public class Emotions
    {
        public EmotionValence facialexpression;
        public EmotionValence physiologicalexpression;
        public EmotionValence feltexpression;
    }

    public class EmotionValence
    {
        public double positive;
        public double negative;
    }

}