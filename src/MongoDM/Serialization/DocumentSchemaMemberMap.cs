using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Digicando.MongoDM.Serialization
{
    public class DocumentSchemaMemberMap
    {
        // Constructors.
        public DocumentSchemaMemberMap(
            Type rootModelType,
            IEnumerable<EntityMember> memberPath,
            DocumentVersion version,
            bool? useCascadeDelete)
        {
            MemberPath = memberPath;
            RootModelType = rootModelType;
            UseCascadeDelete = useCascadeDelete;
            Version = version;
        }

        // Properties.
        public IEnumerable<BsonClassMap> EntityClassMapPath => MemberPath.Select(m => m.EntityClassMap)
                                                                         .Where(cm => cm != null)
                                                                         .Distinct();
        public bool IsIdMember => MemberPath.Last().IsId;
        public bool IsEntityReferenceMember => EntityClassMapPath.Count() >= 2;
        public IEnumerable<EntityMember> MemberPath { get; }
        public IEnumerable<EntityMember> MemberPathToEntity
        {
            get
            {
                var lastEntityNestedMembers = MemberPath.Aggregate<EntityMember, (int counter, BsonClassMap lastEntityClassMap), int>(
                    (1, null),
                    (acc, member) => member.EntityClassMap == acc.lastEntityClassMap ?
                        (acc.counter++, acc.lastEntityClassMap) :
                        (1, member.EntityClassMap),
                    acc => acc.counter);
                var idMemberDeep = MemberPath.Count() - lastEntityNestedMembers;

                return MemberPath.Take(idMemberDeep);
            }
        }
        public IEnumerable<EntityMember> MemberPathToId
        {
            get
            {
                if (IsIdMember)
                    return MemberPath;

                var lastEntityClassMap = MemberPath.Last().EntityClassMap;
                return MemberPathToEntity.Append(new EntityMember(lastEntityClassMap, lastEntityClassMap.IdMemberMap));
            }
        }
        public Type RootModelType { get; }
        public bool? UseCascadeDelete { get; }
        public DocumentVersion Version { get; }

        // Methods.
        public string MemberPathToString() =>
            string.Join(".", MemberPath.Select(member => member.MemberMap.MemberInfo.Name));

        public string FullPathToString() => $"{RootModelType.Name}.{MemberPathToString()}";

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            
            strBuilder.AppendLine(FullPathToString());
            strBuilder.AppendLine($"    version: {Version}");
            strBuilder.AppendLine($"    entity: {string.Join("->", EntityClassMapPath.Select(cm => cm.ClassType.Name))}");
            strBuilder.AppendLine($"    isEntityRefMem: {IsEntityReferenceMember}");
            strBuilder.AppendLine($"    isIdMem: {IsIdMember}");
            strBuilder.AppendLine($"    cascadeDelete: {UseCascadeDelete?.ToString() ?? "null"}");

            return strBuilder.ToString();
        }
    }
}
