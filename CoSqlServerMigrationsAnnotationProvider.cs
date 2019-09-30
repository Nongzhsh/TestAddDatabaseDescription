using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal;

namespace TestAddDatabaseDescription
{
    /// <summary>
    /// 自定义注解无法输出到迁移文件的 Up/Down 中，目前使用此方式解决;
    /// See：https://github.com/aspnet/EntityFrameworkCore/issues/10258;
    /// 继承自 <see cref="SqlServerMigrationsAnnotationProvider"/>
    /// </summary>
    public class CoSqlServerMigrationsAnnotationProvider : SqlServerMigrationsAnnotationProvider
    {
        /// <inheritdoc />
        public CoSqlServerMigrationsAnnotationProvider([NotNull] MigrationsAnnotationProviderDependencies dependencies)
            : base(dependencies)
        {
        }

        /// <inheritdoc />
        public override IEnumerable<IAnnotation> For(IEntityType entityType)
        {
            var baseAnnotations = base.For(entityType);
            var annotation = entityType.FindAnnotation(CoRelationalAnnotationNames.Comment);
            //if (annotation != null)
            //{
            //    System.Diagnostics.Debugger.Launch();
            //}

            return annotation == null
                ? baseAnnotations
                : baseAnnotations.Concat(new[] { annotation });
        }
        
        /// <inheritdoc />
        public override IEnumerable<IAnnotation> For(IProperty property)
        {
            var baseAnnotations = base.For(property);

            var annotation = property.FindAnnotation(CoRelationalAnnotationNames.Comment);

            return annotation == null
                ? baseAnnotations
                : baseAnnotations.Concat(new[] { annotation });
        }
    }
}