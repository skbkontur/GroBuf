using System;
using System.Runtime.Serialization;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class ImmutableObjectChangedBugTest
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void Test()
        {
            Console.WriteLine(DebugViewBuilder.DebugView(serializer.Serialize(new[]
                {
                    new RSV1DisabledPerson {Validation = new ValidationResult("zaa", ValidationResultType.Error)},
                    new RSV1DisabledPerson {Validation = ValidationResult.Ok},
                    new RSV1DisabledPerson {Validation = new ValidationResult("qs", ValidationResultType.Warning)},
                })));
        }

        [Test]
        public void TestValidationResultBug()
        {
            var expected = new[]
                {
                    new RSV1DisabledPerson {Validation = new ValidationResult("zaa", ValidationResultType.Error)},
                    new RSV1DisabledPerson {Validation = ValidationResult.Ok},
                    new RSV1DisabledPerson {Validation = new ValidationResult("qs", ValidationResultType.Warning)},
                };
            Assert.AreEqual(ValidationResultType.Ok, ValidationResult.Ok.Type);
            Assert.AreEqual(null, ValidationResult.Ok.Message);
            var actual = serializer.Deserialize<RSV1DisabledPerson[]>(serializer.Serialize(expected));
            Assert.AreEqual(ValidationResultType.Error, actual[0].Validation.Type);
            Assert.AreEqual("zaa", actual[0].Validation.Message);
            Assert.AreEqual(ValidationResultType.Ok, actual[1].Validation.Type);
            Assert.AreEqual(null, actual[1].Validation.Message);
            Assert.AreEqual(ValidationResultType.Warning, actual[2].Validation.Type);
            Assert.AreEqual("qs", actual[2].Validation.Message);
        }

        private enum ValidationResultType
        {
            Ok = 0,
            Warning = 1,
            Error = 2,
            Fatal = 3
        }

        private Serializer serializer;

        private class ValidationResult
        {
            public ValidationResult(string message, ValidationResultType type)
            {
                Message = message;
                Type = type;
            }

            public string Message { get; private set; }

            public ValidationResultType Type { get; private set; }
            public static readonly ValidationResult Ok = new ValidationResult(null, ValidationResultType.Ok);
        }

        private class RSV1DisabledPerson
        {
            [DataMember]
            public ValidationResult Validation { get { return validation ?? ValidationResult.Ok; } set { validation = value; } }

            private ValidationResult validation;
        }
    }
}