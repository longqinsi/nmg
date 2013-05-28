﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using NMG.Core.Domain;
using System.Text;

namespace NMG.Core.Generator
{
    public class CodeGenerator : AbstractGenerator
    {
        private readonly ApplicationPreferences appPrefs;
        private readonly Language language;

        public CodeGenerator(ApplicationPreferences appPrefs, Table table)
            : base(appPrefs.FolderPath, "Domain", appPrefs.TableName, appPrefs.NameSpace, appPrefs.AssemblyName, appPrefs.Sequence, table, appPrefs)
        {
            this.appPrefs = appPrefs;
            language = appPrefs.Language;
        }

        public string ClassName { get { return string.Format("{0}{1}", appPrefs.ClassNamePrefix, Formatter.FormatSingular(Table.Name)); } }

        public override void Generate()
        {
            var compileUnit = GetCompileUnit(ClassName);
            WriteToFile(compileUnit, ClassName);
        }

        public CodeCompileUnit GetCompileUnit(string className)
        {
            var codeGenerationHelper = new CodeGenerationHelper();
            var compileUnit = codeGenerationHelper.GetCodeCompileUnitWithInheritanceAndInterface(nameSpace, className, appPrefs.InheritenceAndInterfaces);

            var mapper = new DataTypeMapper();
            var newType = compileUnit.Namespaces[0].Types[0];

            newType.IsPartial = appPrefs.GeneratePartialClasses;

            CreateProperties(codeGenerationHelper, mapper, newType);

            var constructorStatements = new CodeStatementCollection();
            foreach (var hasMany in Table.HasManyRelationships)
            {
                newType.Members.Add(codeGenerationHelper.CreateAutoProperty(appPrefs.ForeignEntityCollectionType + "<" + appPrefs.ClassNamePrefix + Formatter.FormatSingular(hasMany.Reference) + ">", Formatter.FormatPlural(hasMany.Reference), appPrefs.UseLazy));
                constructorStatements.Add(new CodeSnippetStatement(string.Format(TABS + "{0} = new {1}<{2}{3}>();", Formatter.FormatPlural(hasMany.Reference), codeGenerationHelper.InstatiationObject(appPrefs.ForeignEntityCollectionType), appPrefs.ClassNamePrefix, Formatter.FormatSingular(hasMany.Reference))));
            }

            var constructor = new CodeConstructor { Attributes = MemberAttributes.Public };
            constructor.Statements.AddRange(constructorStatements);
            newType.Members.Add(constructor);
            return compileUnit;
        }

        private void CreateProperties(CodeGenerationHelper codeGenerationHelper, DataTypeMapper mapper, CodeTypeDeclaration newType)
        {
            switch (appPrefs.FieldGenerationConvention)
            {
                case FieldGenerationConvention.Field:
                    CreateFields(codeGenerationHelper, mapper, newType);
                    break;
                case FieldGenerationConvention.Property:
                    CreateFullProperties(codeGenerationHelper, mapper, newType);
                    break;
                case FieldGenerationConvention.AutoProperty:
                    CreateAutoProperties(codeGenerationHelper, mapper, newType);
                    break;
            }
        }

        private void CreateFields(CodeGenerationHelper codeGenerationHelper, DataTypeMapper mapper, CodeTypeDeclaration newType)
        {
            foreach (var pk in Table.PrimaryKey.Columns)
            {
                var mapFromDbType = mapper.MapFromDBType(this.appPrefs.ServerType, pk.DataType, pk.DataLength, pk.DataPrecision, pk.DataScale);
                newType.Members.Add(codeGenerationHelper.CreateField(mapFromDbType, Formatter.FormatText(pk.Name), true));
            }

            // Note that a foreign key referencing a primary within the same table will end up giving you a foreign key property with the same name as the table.
            foreach (var fk in Table.ForeignKeys.Where(fk => !string.IsNullOrEmpty(fk.References)))
            {
                newType.Members.Add(codeGenerationHelper.CreateField(appPrefs.ClassNamePrefix + Formatter.FormatSingular(fk.References), Formatter.FormatSingular(fk.UniquePropertyName)));
            }

            foreach (var column in Table.Columns.Where(x => x.IsPrimaryKey != true))
            {
                if (!appPrefs.IncludeForeignKeys && column.IsForeignKey)
                    continue;
                var mapFromDbType = mapper.MapFromDBType(this.appPrefs.ServerType, column.DataType, column.DataLength, column.DataPrecision, column.DataScale);
                newType.Members.Add(codeGenerationHelper.CreateField(mapFromDbType, Formatter.FormatText(column.Name), true, column.IsNullable));
            }
        }

        private void CreateFullProperties(CodeGenerationHelper codeGenerationHelper, DataTypeMapper mapper, CodeTypeDeclaration newType)
        {
            foreach (var pk in Table.PrimaryKey.Columns)
            {
                var mapFromDbType = mapper.MapFromDBType(this.appPrefs.ServerType, pk.DataType, pk.DataLength, pk.DataPrecision, pk.DataScale);
                newType.Members.Add(codeGenerationHelper.CreateField(mapFromDbType, Formatter.FormatText(pk.Name), true));
                newType.Members.Add(codeGenerationHelper.CreateProperty(mapFromDbType, Formatter.FormatText(pk.Name), appPrefs.UseLazy));
            }

            // Note that a foreign key referencing a primary within the same table will end up giving you a foreign key property with the same name as the table.
            foreach (var fk in Table.ForeignKeys.Where(fk => !string.IsNullOrEmpty(fk.References)))
            {
                newType.Members.Add(codeGenerationHelper.CreateField(appPrefs.ClassNamePrefix + Formatter.FormatSingular(fk.References), Formatter.FormatSingular(fk.UniquePropertyName)));
                newType.Members.Add(codeGenerationHelper.CreateProperty(appPrefs.ClassNamePrefix + Formatter.FormatSingular(fk.References), Formatter.FormatSingular(fk.UniquePropertyName), appPrefs.UseLazy));
            }

            foreach (var column in Table.Columns.Where(x => x.IsPrimaryKey != true))
            {
                if (!appPrefs.IncludeForeignKeys && column.IsForeignKey)
                    continue;
                var mapFromDbType = mapper.MapFromDBType(this.appPrefs.ServerType, column.DataType, column.DataLength, column.DataPrecision, column.DataScale);
                newType.Members.Add(codeGenerationHelper.CreateField(mapFromDbType, Formatter.FormatText(column.Name), true, column.IsNullable));
                newType.Members.Add(codeGenerationHelper.CreateProperty(mapFromDbType, Formatter.FormatText(column.Name), column.IsNullable, appPrefs.UseLazy));
            }
        }

        private void CreateAutoProperties(CodeGenerationHelper codeGenerationHelper, DataTypeMapper mapper, CodeTypeDeclaration newType)
        {
            foreach (var pk in Table.PrimaryKey.Columns)
            {
                var mapFromDbType = mapper.MapFromDBType(this.appPrefs.ServerType, pk.DataType, pk.DataLength, pk.DataPrecision, pk.DataScale);
                newType.Members.Add(codeGenerationHelper.CreateAutoProperty(mapFromDbType.ToString(), Formatter.FormatText(pk.Name), appPrefs.UseLazy));
            }

            // Note that a foreign key referencing a primary within the same table will end up giving you a foreign key property with the same name as the table.
            foreach (var fk in Table.ForeignKeys.Where(fk => !string.IsNullOrEmpty(fk.References)))
            {
                newType.Members.Add(codeGenerationHelper.CreateAutoProperty(appPrefs.ClassNamePrefix + Formatter.FormatSingular(fk.References), Formatter.FormatSingular(fk.UniquePropertyName), appPrefs.UseLazy));
            }

            foreach (var column in Table.Columns.Where(x => x.IsPrimaryKey != true))
            {
                if (!appPrefs.IncludeForeignKeys && column.IsForeignKey)
                    continue;
                var mapFromDbType = mapper.MapFromDBType(this.appPrefs.ServerType, column.DataType, column.DataLength, column.DataPrecision, column.DataScale);
                newType.Members.Add(codeGenerationHelper.CreateAutoProperty(mapFromDbType, Formatter.FormatText(column.Name), column.IsNullable, appPrefs.UseLazy));
            }
        }

        public void WriteToFile(CodeCompileUnit compileUnit, string className)
        {
            var provider = GetCodeDomProvider();
            var sourceFile = GetCompleteFilePath(provider, className);
            using (provider)
            {
                var streamWriter = new StreamWriter(sourceFile);
                var textWriter = new IndentedTextWriter(streamWriter, "    ");
                using (textWriter)
                {
                    using (streamWriter)
                    {
                        var options = new CodeGeneratorOptions { BlankLinesBetweenMembers = false };
                        provider.GenerateCodeFromCompileUnit(compileUnit, textWriter, options);
                    }
                }
            }
            CleanupGeneratedFile(sourceFile);
        }

        private void CleanupGeneratedFile(string sourceFile)
        {
            string entireContent;
            using (var reader = new StreamReader(sourceFile))
            {
                entireContent = reader.ReadToEnd();
            }
            entireContent = RemoveComments(entireContent);
            entireContent = AddStandardHeader(entireContent);
            entireContent = FixAutoProperties(entireContent);
            using (var writer = new StreamWriter(sourceFile))
            {
                writer.Write(entireContent);
            }
        }

        // Hack : Auto property generator is not there in CodeDom.
        private static string FixAutoProperties(string entireContent)
        {
            // Do NOT mess with this... 
            //Indomitable: Just a little :)
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.Append("        }");
            entireContent = entireContent.Replace(builder.ToString(), "{ }");
            builder.Clear();
            builder.AppendLine("{");
            builder.AppendLine("            get {");
            builder.AppendLine("            }");
            builder.AppendLine("            set {");
            builder.AppendLine("            }");
            builder.Append("        }");
            entireContent = entireContent.Replace(builder.ToString(), "{ get; set; }");
            return entireContent;
        }

        private string AddStandardHeader(string entireContent)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Text;");
            builder.AppendLine("using System.Collections.Generic;");
            if (appPrefs.ForeignEntityCollectionType.Contains("Iesi.Collections"))
                builder.AppendLine("using Iesi.Collections.Generic;");
            builder.Append(entireContent);
            return builder.ToString();
        }

        private static string RemoveComments(string entireContent)
        {
            int end = entireContent.LastIndexOf("----------");
            entireContent = entireContent.Remove(0, end + 10);
            return entireContent;
        }

        private string GetCompleteFilePath(CodeDomProvider provider, string className)
        {
            string fileName = filePath + className;
            return provider.FileExtension[0] == '.'
                       ? fileName + provider.FileExtension
                       : fileName + "." + provider.FileExtension;
        }

        private CodeDomProvider GetCodeDomProvider()
        {
            return language == Language.CSharp ? (CodeDomProvider)new CSharpCodeProvider() : new VBCodeProvider();
        }
    }
}