using flexop.Api.Dtos;
using static PCM.Core.Interfacing;
using System.Numerics;

namespace flexop.Api.Mappers
{
    public sealed class InputMapper : MapperBase<Input, WorldStateDto>
    {
        public override Input Map(WorldStateDto element)
        {
            return new Input
            {
                TimeStamp = element.TimeStamp, 
                Agents = MapAgentInputs(element.Agents),
                Objects = MapEntities(element.Objects),
                PlayerEmotion = null,
            };
        }

        private static Dictionary<int, AgentInput> MapAgentInputs(Dictionary<int, AgentInputDto> agentInputs)
        {
            Dictionary<int, AgentInput> mappedAgents = new();

            foreach (var agentInput in agentInputs)
            {
                mappedAgents.Add(agentInput.Key, MapAgentInput(agentInput.Value));
            }

            return mappedAgents;
        }

        private static AgentInput MapAgentInput(AgentInputDto agentInputDto)
        {
            return new AgentInput
            {
                Id = agentInputDto.Id,
                Body = MapBody(agentInputDto.Body),
                Emotions = MapEmotionSystem(agentInputDto.Emotions),
                TargetEntityId = agentInputDto.TargetEntityId,
            };
        }

        private static Body MapBody(BodyDto bodyDto)
        {
            return new Body
            {
                Width = bodyDto.Width,
                Height = bodyDto.Height,
                Depth = bodyDto.Depth,
                Center = new Vector3(bodyDto.Center.X, bodyDto.Center.Y, bodyDto.Center.Z),
                OrientationOrigin = new Vector3(bodyDto.OrientationOrigin.X, bodyDto.OrientationOrigin.Y, bodyDto.OrientationOrigin.Z),
                Orientation = new Vector3(bodyDto.Orientation.X, bodyDto.Orientation.Y, bodyDto.Orientation.Z),
            };
        }

        private static EmotionSystem MapEmotionSystem(EmotionSystemDto emotionSystemDto)
        {
            return new EmotionSystem
            {
                felt = MapEmotion(emotionSystemDto.Felt),
                facial = MapEmotion(emotionSystemDto.Facial),
                physiological = MapEmotion(emotionSystemDto.Physiological),
                voluntaryFacial = MapEmotion(emotionSystemDto.VoluntaryFacial),
                voluntaryPhysiological = MapEmotion(emotionSystemDto.VoluntaryPhysiological),
            };
        }

        private static Emotion MapEmotion(EmotionDto emotionDto)
        {
            return new Emotion
            {
                Valence = emotionDto.Valence,
                Positive = emotionDto.Positive,
                Negative = emotionDto.Negative,
                Surprise = emotionDto.Surprise,
            };
        }

        public override WorldStateDto Map(Input element)
        {
            return new WorldStateDto
            {
                //UserName = element.Name,
                //UserAddress = element.Address
            };
        }

        private static Dictionary<int, Entity> MapEntities(Dictionary<int, EntityDto> objects)
        {
            Dictionary<int, Entity> mappedObjects = new();

            foreach (var item in objects)
            {
                mappedObjects.Add(item.Key, MapEntity(item.Value));
            }

            return mappedObjects;
        }

        private static Entity MapEntity(EntityDto entityDto)
        {
            return new Entity
            {
                Id = entityDto.Id,
                Body = MapBody(entityDto.Body),
            };
        }
    }
}
