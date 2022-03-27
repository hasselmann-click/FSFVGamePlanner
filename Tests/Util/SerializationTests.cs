
using FSFV.Gameplanner.Service.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text.Json;
using static FSFV.Gameplanner.Service.SlotService;

namespace Tests.Util
{
    [TestClass]
    public class SerializationTests
    {

        [TestMethod]
        public void SerializeAndDeserialize()
        {
            List<object> ToSerialize = new()
            {
                new Pitch
                {
                    StartTime = DateTime.Parse("10:00"),
                    EndTime = DateTime.Parse("18:00"),
                    Type = new PitchType
                    {
                        PitchTypeID = 1
                    }
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "EA1" } },
                    Away = new Team { Type = new TeamType { Name = "EA2" } },
                    Group = new Grouping
                    {
                        GroupingID = 1,
                        Priority = 100,
                        Type = new GroupType { GroupTypeID = 1 }
                    },
                    MinDuration = TimeSpan.FromMinutes(85)
                }
                // gameday
            };

            var serialized = FsfvJsonSerializer.Serialize(ToSerialize);
            var deserialized = FsfvJsonSerializer.Deserialize<List<object>>(serialized);

            for (int i = 0; i < ToSerialize.Count; ++i)
            {
                var value = ((JsonElement)deserialized[i]).Deserialize(ToSerialize[i].GetType(), FsfvJsonSerializer.Options);
                Assert.AreEqual(ToSerialize[i].GetType(), value?.GetType());
            }

        }

        [TestMethod]
        public void SerializeAndDeserializeToFile()
        {

        }

    }
}
