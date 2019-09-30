using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace TestAddDatabaseDescription
{
    /// <summary>
    /// 为 <see cref="ModelBuilder" /> 提供一些扩展方法
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// 配置数据库表和列说明
        /// </summary>
        /// <param name="modelBuilder"> 模型构造器 </param>
        /// <returns> 模型构造器 </returns>
        [NotNull]
        public static ModelBuilder ConfigDatabaseDescription([NotNull] this ModelBuilder modelBuilder)
        {
            /*TODO：目前根据实体及其属性标注的 DescriptionAttribute 来添加数据库表和列的说明，是否应该使用自定义的 Attribute 或者是根据 Xml 文档来实现呢？如果是根据 Xml 文档，那需要再写个文档读取器，同时需要约定文档路径，感觉不是很好。（对于数据库表和字段说明，我觉得只是针对个别表和字段，并不需要全部添加说明 ...）*/
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                //添加表说明
                var entityDescAttr = entityType.ClrType?.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (entityType.FindAnnotation(CoRelationalAnnotationNames.Comment) == null && entityDescAttr != null)
                {
                    entityType.AddAnnotation(CoRelationalAnnotationNames.Comment, entityDescAttr.Description);
                }

                //添加列说明
                foreach (var property in entityType.GetProperties())
                {
                    var propertyInfo = property.PropertyInfo;
                    if (propertyInfo == null)
                    {
                        continue;
                    }

                    var propertyDescAttr = propertyInfo.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;

                    if (property.FindAnnotation(CoRelationalAnnotationNames.Comment) != null)
                    {
                        continue;
                    }

                    var propertyType = propertyInfo.PropertyType;
                    if (propertyDescAttr == null)
                    {
                        propertyDescAttr = propertyType.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
                    }

                    if (propertyDescAttr == null)
                    {
                        continue;
                    }

                    //如果该列的实体属性是枚举类型，把枚举的说明追加到列说明
                    var propDescription = propertyDescAttr.Description;
                    if (propertyType.IsEnum ||
                        propertyType.IsAssignableFrom(typeof(Nullable<>)) &&
                        propertyType.GenericTypeArguments[0].IsEnum)
                    {
                        var @enum = propertyType.IsAssignableFrom(typeof(Nullable<>))
                            ? propertyType.GenericTypeArguments[0]
                            : propertyType;

                        var descList = new List<string>();
                        foreach (var field in @enum?.GetFields() ?? Array.Empty<FieldInfo>())
                        {
                            if (field.IsSpecialName)
                            {
                                continue;
                            }

                            var fieldDescAttr = field.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
                            var description = fieldDescAttr?.Description;

                            descList.Add($@"{field.GetRawConstantValue()}: {(string.IsNullOrWhiteSpace(description) ? field.Name : description)}");
                        }

                        // 添加 Flag 说明
                        var isFlags = @enum?.GetCustomAttribute(typeof(FlagsAttribute)) != null;
                        var enumTypeDescription = isFlags ? "[标志位枚举]" : string.Empty;

                        propDescription += $@"({enumTypeDescription}{string.Join("; ", descList)})";
                    }

                    property.AddAnnotation(CoRelationalAnnotationNames.Comment, $@"{propDescription}");
                }
            }

            return modelBuilder;
        }
    }
}