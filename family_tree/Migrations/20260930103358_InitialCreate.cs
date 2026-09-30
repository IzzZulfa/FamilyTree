using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace family_tree.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "family_trees",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_family_trees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tree_id = table.Column<int>(type: "integer", nullable: false),
                    given_name = table.Column<string>(type: "text", nullable: false),
                    surname = table.Column<string>(type: "text", nullable: false),
                    birth_surname = table.Column<string>(type: "text", nullable: true),
                    sex = table.Column<string>(type: "text", nullable: false),
                    birth_date_value = table.Column<DateOnly>(type: "date", nullable: true),
                    birth_date_precision = table.Column<string>(type: "text", nullable: true),
                    birth_place = table.Column<string>(type: "text", nullable: true),
                    death_date_value = table.Column<DateOnly>(type: "date", nullable: true),
                    death_date_precision = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_people", x => x.id);
                    table.ForeignKey(
                        name: "fk_people_family_trees_tree_id",
                        column: x => x.tree_id,
                        principalTable: "family_trees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parent_children",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_id = table.Column<int>(type: "integer", nullable: false),
                    child_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parent_children", x => x.id);
                    table.ForeignKey(
                        name: "fk_parent_children_people_child_id",
                        column: x => x.child_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_parent_children_people_parent_id",
                        column: x => x.parent_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "partnerships",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person1_id = table.Column<int>(type: "integer", nullable: false),
                    person2_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    start_date_value = table.Column<DateOnly>(type: "date", nullable: true),
                    start_date_precision = table.Column<string>(type: "text", nullable: true),
                    end_date_value = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date_precision = table.Column<string>(type: "text", nullable: true),
                    end_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partnerships", x => x.id);
                    table.ForeignKey(
                        name: "fk_partnerships_people_person1id",
                        column: x => x.person1_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_partnerships_people_person2id",
                        column: x => x.person2_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_parent_children_child_id",
                table: "parent_children",
                column: "child_id");

            migrationBuilder.CreateIndex(
                name: "ix_parent_children_parent_id_child_id",
                table: "parent_children",
                columns: new[] { "parent_id", "child_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_partnerships_person1id",
                table: "partnerships",
                column: "person1_id");

            migrationBuilder.CreateIndex(
                name: "ix_partnerships_person2id",
                table: "partnerships",
                column: "person2_id");

            migrationBuilder.CreateIndex(
                name: "ix_people_tree_id",
                table: "people",
                column: "tree_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parent_children");

            migrationBuilder.DropTable(
                name: "partnerships");

            migrationBuilder.DropTable(
                name: "people");

            migrationBuilder.DropTable(
                name: "family_trees");
        }
    }
}
