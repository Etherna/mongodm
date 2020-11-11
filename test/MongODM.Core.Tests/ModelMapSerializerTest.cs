//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Etherna.MongODM.Core.Comparers;
using Etherna.MongODM.Core.Models;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Mapping.Schemas;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Serialization.Serializers;
using Etherna.MongODM.Core.Utility;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Etherna.MongODM.Core
{
    public class ModelMapSerializerTest
    {
        // Internal classes.
        public class DeserializationTestElement
        {
            public DeserializationTestElement(
                BsonDocument document,
                FakeModel? expectedModel,
                Action<BsonReader>? preAction = null,
                Action<BsonReader>? postAction = null)
            {
                Document = document;
                ExpectedModel = expectedModel;
                PreAction = preAction ?? (rd => { });
                PostAction = postAction ?? (rd => { });
            }

            public BsonDocument Document { get; }
            public FakeModel? ExpectedModel { get; }
            public Action<BsonReader> PreAction { get; }
            public Action<BsonReader> PostAction { get; }
        }
        public class SerializationTestElement
        {
            public SerializationTestElement(
                FakeModel? model,
                BsonDocument expectedDocument,
                Action<BsonWriter>? preAction = null,
                Action<BsonWriter>? postAction = null)
            {
                BsonWriter = new BsonDocumentWriter(SerializedDocument);
                ExpectedDocument = expectedDocument;
                Model = model;
                PreAction = preAction ?? (wr => { });
                PostAction = postAction ?? (wr => { });
            }

            public BsonWriter BsonWriter { get; }
            public BsonDocument ExpectedDocument { get; }
            public FakeModel? Model { get; }
            public Action<BsonWriter> PreAction { get; }
            public Action<BsonWriter> PostAction { get; }
            public BsonDocument SerializedDocument { get; } = new BsonDocument();
        }

        // Fields.
        private readonly Mock<IDbCache> dbCacheMock = new();
        private readonly DocumentSemVerOptions documentSemVerOptions = new();
        private readonly Mock<IModelMapsSchema<FakeModel>> modelMapsSchemaMock = new();
        private readonly ModelMapVersionOptions modelMapVersionOptions = new();
        private readonly Mock<ISchemaRegister> schemaRegisterMock = new();
        private readonly Mock<ISerializerModifierAccessor> serializerModifierAccessorMock = new();

        // Constructor.
        public ModelMapSerializerTest()
        {
            dbCacheMock.Setup(c => c.LoadedModels.ContainsKey(It.IsAny<object>()))
                .Returns(() => false);

            schemaRegisterMock.Setup(sr => sr.GetModelMapsSchema<FakeModel>())
                .Returns(() => modelMapsSchemaMock.Object);
        }

        // Tests.
        public static IEnumerable<object[]> DeserializationTests
        {
            get
            {
                var tests = new List<DeserializationTestElement>
                {
                    // Null model
                    new DeserializationTestElement(
                        new BsonDocument(new BsonElement("elem", BsonNull.Value)),
                        null,
                        preAction: rd =>
                        {
                            rd.ReadStartDocument();
                            rd.ReadName();
                        },
                        postAction: rd => rd.ReadEndDocument()),

                    // Model without extra members
                    new DeserializationTestElement(
                        new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("IntegerProp", new BsonInt32(8)),
                            new BsonElement("StringProp", new BsonString("ok"))
                        } as IEnumerable<BsonElement>),
                        new FakeModel
                        {
                            Id = "idVal",
                            IntegerProp = 8,
                            StringProp = "ok"
                        })
                };
                return tests.Select(t => new object[] { t });
            }
        }

        [Theory, MemberData(nameof(DeserializationTests))]
        public void Deserialize(DeserializationTestElement test)
        {
            // Setup
            var bsonReader = new BsonDocumentReader(test.Document);
            var bsonClassMapSerializer = CreateBsonClassMapSerializer();
            var serializer = new ModelMapSerializer<FakeModel>(
                dbCacheMock.Object,
                documentSemVerOptions,
                modelMapVersionOptions,
                schemaRegisterMock.Object,
                serializerModifierAccessorMock.Object);

            modelMapsSchemaMock.Setup(s => s.ActiveMap.BsonClassMapSerializer)
                .Returns(bsonClassMapSerializer);

            // Action
            test.PreAction(bsonReader);
            var result = serializer.Deserialize(
                BsonDeserializationContext.CreateRoot(bsonReader),
                new BsonDeserializationArgs { NominalType = typeof(FakeModel) });
            test.PostAction(bsonReader);

            // Assert
            Assert.Equal(test.ExpectedModel, result, new FakeModelComparer());
        }

        [Fact]
        public void GetDocumentId()
        {
            // Setup
            var model = new FakeModel { Id = "idVal" };
            var bsonClassMapSerializer = CreateBsonClassMapSerializer();
            var serializer = new ModelMapSerializer<FakeModel>(
                dbCacheMock.Object,
                documentSemVerOptions,
                modelMapVersionOptions,
                schemaRegisterMock.Object,
                serializerModifierAccessorMock.Object);

            modelMapsSchemaMock.Setup(s => s.ActiveMap.BsonClassMapSerializer)
                .Returns(bsonClassMapSerializer);

            // Action
            var result = serializer.GetDocumentId(
                model,
                out object idResult,
                out Type idNominalTypeResult,
                out IIdGenerator idGeneratorResult);

            // Assert
            var resultExpected = bsonClassMapSerializer.GetDocumentId(
                model,
                out object idExpected,
                out Type idNominalTypeExpected,
                out IIdGenerator idGeneratorExpected);

            Assert.Equal(idExpected, idResult);
            Assert.Equal(idNominalTypeExpected, idNominalTypeResult);
            Assert.Equal(idGeneratorExpected, idGeneratorResult);
            Assert.Equal(resultExpected, result);
        }

        [Fact]
        public void GetMemberSerializationInfo()
        {
            // Setup
            var memberName = nameof(FakeModel.StringProp);
            var bsonClassMapSerializer = CreateBsonClassMapSerializer();
            var serializer = new ModelMapSerializer<FakeModel>(
                dbCacheMock.Object,
                documentSemVerOptions,
                modelMapVersionOptions,
                schemaRegisterMock.Object,
                serializerModifierAccessorMock.Object);

            modelMapsSchemaMock.Setup(s => s.ActiveMap.BsonClassMapSerializer)
                .Returns(bsonClassMapSerializer);

            // Action
            var result = serializer.TryGetMemberSerializationInfo(memberName, out BsonSerializationInfo serializationInfo);

            // Assert
            var expectedResult = bsonClassMapSerializer.TryGetMemberSerializationInfo(memberName,
                out BsonSerializationInfo expectedSerializationInfo);
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedSerializationInfo.ElementName, serializationInfo.ElementName);
            Assert.Equal(expectedSerializationInfo.NominalType, serializationInfo.NominalType);
        }

        public static IEnumerable<object[]> SerializationTests
        {
            get
            {
                var tests = new List<SerializationTestElement>
                {
                    // Null model
                    new SerializationTestElement(
                        null,
                        new BsonDocument(new BsonElement("elem", BsonNull.Value)),
                        preAction: wr =>
                        {
                            wr.WriteStartDocument();
                            wr.WriteName("elem");
                        },
                        postAction: wr => wr.WriteEndDocument()),

                    // Complex model
                    new SerializationTestElement(
                        new FakeModel()
                        {
                            EnumerableProp = new[] { new FakeModel(), null },
                            Id = "idVal",
                            IntegerProp = 42,
                            ObjectProp = new FakeModel(),
                            StringProp = "yes"
                        },
                        new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_m", new BsonString("mapId")),
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                            new BsonElement("EnumerableProp", new BsonArray(new BsonValue[]
                            {
                                new BsonDocument(new BsonElement[]
                                {
                                    new BsonElement("_m", new BsonString("mapId")),
                                    new BsonElement("_id", BsonNull.Value),
                                    new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                                    new BsonElement("EnumerableProp", BsonNull.Value),
                                    new BsonElement("IntegerProp", new BsonInt32(0)),
                                    new BsonElement("ObjectProp", BsonNull.Value),
                                    new BsonElement("StringProp", BsonNull.Value)
                                } as IEnumerable<BsonElement>),
                                BsonNull.Value
                            })),
                            new BsonElement("IntegerProp", new BsonInt32(42)),
                            new BsonElement("ObjectProp", new BsonDocument(new BsonElement[]
                            {
                                new BsonElement("_m", new BsonString("mapId")),
                                new BsonElement("_id", BsonNull.Value),
                                new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                                new BsonElement("EnumerableProp", BsonNull.Value),
                                new BsonElement("IntegerProp", new BsonInt32(0)),
                                new BsonElement("ObjectProp", BsonNull.Value),
                                new BsonElement("StringProp", BsonNull.Value)
                            } as IEnumerable<BsonElement>)),
                            new BsonElement("StringProp", new BsonString("yes")),
                        } as IEnumerable<BsonElement>)),
                };

                return tests.Select(t => new object[] { t });
            }
        }

        [Theory, MemberData(nameof(SerializationTests))]
        public void Serialize(SerializationTestElement test)
        {
            // Setup
            var bsonClassMapSerializer = CreateBsonClassMapSerializer();
            var serializer = new ModelMapSerializer<FakeModel>(
                dbCacheMock.Object,
                documentSemVerOptions,
                modelMapVersionOptions,
                schemaRegisterMock.Object,
                serializerModifierAccessorMock.Object);

            modelMapsSchemaMock.Setup(s => s.ActiveMap.BsonClassMapSerializer)
                .Returns(bsonClassMapSerializer);
            modelMapsSchemaMock.Setup(s => s.ActiveMap.Id)
                .Returns("mapId");

            // Action
            test.PreAction(test.BsonWriter);
            serializer.Serialize(
                BsonSerializationContext.CreateRoot(test.BsonWriter),
                new BsonSerializationArgs { NominalType = typeof(FakeModel) },
                test.Model!);
            test.PostAction(test.BsonWriter);

            // Assert
            Assert.Equal(0, test.SerializedDocument.CompareTo(test.ExpectedDocument));
        }

        [Fact]
        public void SetDocumentId()
        {
            // Setup
            var id = "idVal";
            var model = new FakeModel();
            var bsonClassMapSerializer = CreateBsonClassMapSerializer();
            var serializer = new ModelMapSerializer<FakeModel>(
                dbCacheMock.Object,
                documentSemVerOptions,
                modelMapVersionOptions,
                schemaRegisterMock.Object,
                serializerModifierAccessorMock.Object);

            modelMapsSchemaMock.Setup(s => s.ActiveMap.BsonClassMapSerializer)
                .Returns(bsonClassMapSerializer);

            // Action
            serializer.SetDocumentId(model, id);

            // Assert
            var expectedModel = new FakeModel();
            bsonClassMapSerializer.SetDocumentId(expectedModel, id);
            Assert.Equal(expectedModel, model, new FakeModelComparer());
        }

        private static BsonClassMapSerializer<FakeModel> CreateBsonClassMapSerializer()
        {
            var classMap = new BsonClassMap<FakeModel>(cm => cm.AutoMap());
            classMap.Freeze();
            return new BsonClassMapSerializer<FakeModel>(classMap);
        }
    }
}
