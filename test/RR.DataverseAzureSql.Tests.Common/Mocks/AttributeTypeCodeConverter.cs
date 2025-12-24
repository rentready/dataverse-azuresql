using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using RR.Common.General;
using RR.Common.Validations;

namespace RR.DataverseAzureSql.Tests.Common.Mocks
{
    public class AttributeTypeCodeConverter
    {
        private readonly string _entityName;
        private readonly LazyWithoutExceptionCaching<Type> _lazyGetEntityType;

        public AttributeTypeCodeConverter(string entityName)
        {
            _entityName = entityName.IsNotNull(nameof(entityName));
            _lazyGetEntityType = new LazyWithoutExceptionCaching<Type>(GetEntityType);
        }

        public AttributeTypeCode ConvertObjectToAttributeTypeCode(object obj, string propertyName)
        {
            var type = GetPropertyTypeFromMeatadata(propertyName);
            if (type == null)
            {
                obj.IsNotNull(nameof(obj));
                if (obj is EntityReference)
                    return AttributeTypeCode.Lookup;
                if (obj is OptionSetValue)
                    return AttributeTypeCode.Integer;
                if (obj is Money)
                    return AttributeTypeCode.Decimal;
                type = obj.GetType();
            }
            else
            {
                if (type == typeof(EntityReference))
                    return AttributeTypeCode.Lookup;
                if (type == typeof(OptionSetValue))
                    return AttributeTypeCode.Integer;
                if (type == typeof(Money))
                    return AttributeTypeCode.Decimal;
            }
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            if (underlyingType == typeof(object))
            {
                if (obj is EntityReference)
                    return AttributeTypeCode.Lookup;
                if (obj is OptionSetValue)
                    return AttributeTypeCode.Integer;
                if (obj is Money)
                    return AttributeTypeCode.Decimal;
                underlyingType = obj.GetType();
            }
            if (underlyingType == typeof(Guid))
            {
                return AttributeTypeCode.Uniqueidentifier;
            }

            var typeCode = Type.GetTypeCode(underlyingType);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return AttributeTypeCode.Boolean;
                case TypeCode.DateTime:
                    return AttributeTypeCode.DateTime;
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return AttributeTypeCode.Decimal;
                case TypeCode.Int32:
                    return AttributeTypeCode.Integer;
                case TypeCode.Int64:
                    return AttributeTypeCode.BigInt;
                case TypeCode.String:
                    return AttributeTypeCode.String;
                // Add other cases as needed
                default:
                    throw new ArgumentException($"Unsupported object type. {typeCode}");
            }
        }

        private Type GetPropertyTypeFromMeatadata(string propertyName)
        {
            var entityType = _lazyGetEntityType.Value;
            if (entityType == null)
            {
                return null;
            }
            var property = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .FirstOrDefault(x => x.GetCustomAttribute<AttributeLogicalNameAttribute>()?.LogicalName == propertyName);
            if (property == null)
            {
                throw new ArgumentException(propertyName);
            }
            return property.PropertyType;
        }

        private Type GetEntityType()
        {
            var modelAssembly = Assembly.GetAssembly(typeof(RR.Entities.Models.msdyn_workorder));
            var modelNamespace = typeof(RR.Entities.Models.Account).Namespace;
            var modelTypes = GetTypesInNamespace(modelAssembly, modelNamespace);
            var entityType = Array.Find(modelTypes, x => string.Equals(x.Name, _entityName, StringComparison.OrdinalIgnoreCase));
            return entityType;
        }

        private static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(t => string.Equals(t.Namespace, nameSpace, StringComparison.OrdinalIgnoreCase))
                      .ToArray();
        }
    }
}
