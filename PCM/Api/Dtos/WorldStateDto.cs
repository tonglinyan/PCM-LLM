namespace flexop.Api.Dtos
{
    public class WorldStateDto
    {
        public long TimeStamp { get; set; }
        public Dictionary<int, AgentInputDto> Agents { get; set; }
        public Dictionary<int, EntityDto> Objects { get; set; }
        public PlayerEmotionDto? PlayerEmotion { get; set; }
    }

    public class PlayerEmotionDto
    {
        public double valence { get; set; }
        public Dictionary<int, double> valenceFactor { get; set; }
    }

    public class EntityDto
    {
        public BodyDto Body { get; set; }
        public int Id { get; set; }
    }

    public class BodyDto
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Depth { get; set; }
        public CustomVector3 Center { get; set; }
        public CustomVector3 OrientationOrigin { get; set; }
        public CustomVector3 Orientation { get; set; }
    }

    public class EmotionDto
    {
        public double Valence { get; set; }
        public double Positive { get; set; }
        public double Negative { get; set; }
        public double Surprise { get; set; }
    }

    public class EmotionSystemDto
    {
        public EmotionDto Felt { get; set; }
        public EmotionDto Facial { get; set; }
        public EmotionDto Physiological { get; set; }
        public EmotionDto VoluntaryFacial { get; set; }
        public EmotionDto VoluntaryPhysiological { get; set; }
    }

    public class AgentInputDto : EntityDto
    {
        public EmotionSystemDto Emotions { get; set; }
        public int TargetEntityId { get; set; }
    }

    public class CustomVector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}