using Valve.Newtonsoft.Json;
using PCM.Verbal.LMM.Parameters;

namespace PCM.Verbal.LLM
{
    public class ContextPrompt
    {
        public string[] QAInstruction { get; set; }

        public string UserContext { get; set; }

        public string[] PUInstruction { get; set; }

        public string[] APInstruction { get; set; }
        
        public string[] LastQuestion { get; set; }
    }

    public partial class ContextPromptParser
    {
        public static ContextPrompt FromJson(string json) => JsonConvert.DeserializeObject<ContextPrompt>(json, Converter.Settings);
        public static string ReadJsonFromFile(string filename)
        {
            using StreamReader file = new(filename);
            return file.ReadToEnd();
        }
        public static ContextPrompt FromFile(string filename) => FromJson(ReadJsonFromFile(filename));
    }
}

