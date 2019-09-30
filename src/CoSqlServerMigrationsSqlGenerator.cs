using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace TestAddDatabaseDescription
{
    /// <summary>
    /// 继承自 <see cref="CoSqlServerMigrationsSqlGenerator"/>；
    /// 重载了一些方法，以便能够处理数据库表和列的说明。
    /// </summary>
    public class CoSqlServerMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
    {
        /// <inheritdoc />
        public CoSqlServerMigrationsSqlGenerator(
            [NotNull] MigrationsSqlGeneratorDependencies dependencies,
            [NotNull] IMigrationsAnnotationProvider migrationsAnnotations)
            : base(dependencies, migrationsAnnotations)
        {
        }

        /// <inheritdoc />
        protected override void Generate(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate)
        {
            base.Generate(operation, model, builder, terminate);

            var comment = GetComment(operation);
            if (comment != null)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);

                AddDescription(
                    builder,
                    comment,
                    operation.Schema,
                    operation.Table,
                    operation.Name);

                builder.EndCommand(suppressTransaction: IsMemoryOptimized(operation, model, operation.Schema, operation.Table));
            }
        }

        /// <inheritdoc />
        protected override void Generate(AlterColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            base.Generate(operation, model, builder);

            var oldComment = GetComment(operation.OldColumn);
            var comment = GetComment(operation);
            if (oldComment != comment)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                var dropDescription = oldComment != null;
                if (dropDescription)
                {
                    DropDescription(
                        builder,
                        operation.Schema,
                        operation.Table,
                        operation.Name);
                }

                if (comment != null)
                {
                    AddDescription(
                        builder,
                        comment,
                        operation.Schema,
                        operation.Table,
                        operation.Name,
                        omitSchemaVariable: dropDescription);
                }
                builder.EndCommand(suppressTransaction: IsMemoryOptimized(operation, model, operation.Schema, operation.Table));
            }
        }

        /// <inheritdoc />
        protected override void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            base.Generate(operation, model, builder);

            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            var comment = GetComment(operation);
            System.Diagnostics.Debugger.Launch();
            var firstDescription = true;
            if (comment != null)
            {
                AddDescription(builder, comment, operation.Schema, operation.Name);
                firstDescription = false;
            }

            foreach (var column in operation.Columns)
            {
                comment = GetComment(column);
                if (comment != null)
                {
                    AddDescription(
                        builder,
                        comment,
                        operation.Schema,
                        operation.Name,
                        column.Name,
                        omitSchemaVariable: !firstDescription);

                    firstDescription = false;
                }
            }
            var memoryOptimized = IsMemoryOptimized(operation);
            builder.EndCommand(suppressTransaction: memoryOptimized);
        }

        /// <inheritdoc />
        protected override void Generate(AlterTableOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            base.Generate(operation, model, builder);

            System.Diagnostics.Debugger.Launch();
            var oldComment = GetComment(operation.OldTable);
            var comment = GetComment(operation);
            if (oldComment != comment)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);

                var dropDescription = oldComment != null;
                if (dropDescription)
                {
                    DropDescription(builder, operation.Schema, operation.Name);
                }

                if (comment != null)
                {
                    AddDescription(
                        builder,
                        comment,
                        operation.Schema,
                        operation.Name,
                        omitSchemaVariable: dropDescription);
                }
                builder.EndCommand(suppressTransaction: IsMemoryOptimized(operation, model, operation.Schema, operation.Name));
            }
        }

        /// <summary>
        ///     <para>
        ///         Generates add commands for descriptions on tables and columns.
        ///     </para>
        /// </summary>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        /// <param name="description"> The new description to be applied. </param>
        /// <param name="schema"> The schema of the table. </param>
        /// <param name="table"> The name of the table. </param>
        /// <param name="column"> The name of the column. </param>
        /// <param name="omitSchemaVariable">
        ///     Indicates whether the @defaultSchema variable declaraion should be omitted.
        /// </param>
        protected virtual void AddDescription(
            [NotNull] MigrationCommandListBuilder builder,
            [CanBeNull] string description,
            [CanBeNull] string schema,
            [NotNull] string table,
            [CanBeNull] string column = null,
            bool omitSchemaVariable = false)
        {
            var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));

            string schemaLiteral;
            if (schema == null)
            {
                if (!omitSchemaVariable)
                {
                    builder.Append("DECLARE @defaultSchema AS sysname")
                        .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                    builder.Append("SET @defaultSchema = SCHEMA_NAME()")
                        .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                }

                schemaLiteral = "@defaultSchema";
            }
            else
            {
                schemaLiteral = Literal(schema);
            }

            builder
                .Append("EXEC sp_addextendedproperty 'MS_Description', ")
                .Append(Literal(description))
                .Append(", 'SCHEMA', ")
                .Append(schemaLiteral)
                .Append(", 'TABLE', ")
                .Append(Literal(table));

            if (column != null)
            {
                builder
                    .Append(", 'COLUMN', ")
                    .Append(Literal(column));
            }

            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);

            string Literal(string s) => stringTypeMapping.GenerateSqlLiteral(s);
        }

        /// <summary>
        ///     <para>
        ///         Generates drop commands for descriptions on tables and columns.
        ///     </para>
        /// </summary>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        /// <param name="schema"> The schema of the table. </param>
        /// <param name="table"> The name of the table. </param>
        /// <param name="column"> The name of the column. </param>
        /// <param name="omitSchemaVariable">
        ///     Indicates whether the @defaultSchema variable declaraion should be omitted.
        /// </param>
        protected virtual void DropDescription(
            [NotNull] MigrationCommandListBuilder builder,
            [CanBeNull] string schema,
            [NotNull] string table,
            [CanBeNull] string column = null,
            bool omitSchemaVariable = false)
        {
            var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));

            string schemaLiteral;
            if (schema == null)
            {
                if (!omitSchemaVariable)
                {
                    builder.Append("DECLARE @defaultSchema AS sysname")
                        .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                    builder.Append("SET @defaultSchema = SCHEMA_NAME()")
                        .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                }

                schemaLiteral = "@defaultSchema";
            }
            else
            {
                schemaLiteral = Literal(schema);
            }

            builder
                .Append("EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', ")
                .Append(schemaLiteral)
                .Append(", 'TABLE', ")
                .Append(Literal(table));

            if (column != null)
            {
                builder
                    .Append(", 'COLUMN', ")
                    .Append(Literal(column));
            }

            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);

            string Literal(string s) => stringTypeMapping.GenerateSqlLiteral(s);
        }

        /// <summary>
        /// 获取说明备注
        /// </summary>
        /// <param name="annotatable"> <see cref="IMutableAnnotatable"/> 实例 </param>
        /// <returns></returns>
        [CanBeNull]
        private static string GetComment([NotNull] IMutableAnnotatable annotatable)
        {
            return annotatable[CoRelationalAnnotationNames.Comment]?.ToString();
        }

        private bool IsMemoryOptimized([NotNull] IAnnotatable annotatable, [CanBeNull] IModel model, [CanBeNull] string schema, [NotNull] string tableName)
            => annotatable[SqlServerAnnotationNames.MemoryOptimized] as bool?
               ?? FindEntityTypes(model, schema, tableName)?.Any(t => t.SqlServer().IsMemoryOptimized) == true;

        private static bool IsMemoryOptimized([NotNull] IAnnotatable annotatable)
            => annotatable[SqlServerAnnotationNames.MemoryOptimized] as bool? == true;
    }
}