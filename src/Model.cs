using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TestAddDatabaseDescription
{
    public class BloggingContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.ReplaceService<IMigrationsAnnotationProvider, CoSqlServerMigrationsAnnotationProvider>();
            options.ReplaceService<IMigrationsSqlGenerator, CoSqlServerMigrationsSqlGenerator>();
            options.UseSqlServer("Server=localhost; Database=TestAddDatabaseDescription; Trusted_Connection=True;");
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigDatabaseDescription();
        }
    }

    [Description("Blog 表")]
    public class Blog
    {
        public int BlogId { get; set; }

        [Description("Url 地址")] 
        public string Url { get; set; }

        public List<Post> Posts { get; } = new List<Post>();
    }

    [Description("博客文章类型")]
    public enum PostType
    {
        [Description("大数据")] 
        BigData,

        [Description("小数据")] 
        SmallData
    }

    [Description("文章")]
    public class Post
    {
        public int PostId { get; set; }

        [Description("文章标题")]
        public string Title { get; set; }

        public PostType PostType { get; set; }

        [Description("文章内容")]
        public string Content { get; set; }

        public int BlogId { get; set; }
        public Blog Blog { get; set; }
    }
}