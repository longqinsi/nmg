using NMG.Core;
using NMG.Core.Domain;
using NMG.Core.Generator;

namespace NHibernateMappingGenerator
{
    public class ApplicationController
    {
        private readonly ApplicationPreferences applicationPreferences;
        private readonly CastleGenerator castleGenerator;
        private readonly CodeGenerator codeGenerator;
        private readonly FluentGenerator fluentGenerator;
        private readonly NHFluentGenerator nhFluentGenerator;
        private readonly MappingGenerator mappingGenerator;
        private readonly ContractGenerator contractGenerator;
        private readonly ByCodeGenerator byCodeGenerator;

        public ApplicationController(ApplicationPreferences applicationPreferences, Table table)
        {
            this.applicationPreferences = applicationPreferences;
            codeGenerator = new CodeGenerator(applicationPreferences, table);
            fluentGenerator = new FluentGenerator(applicationPreferences, table);
            nhFluentGenerator = new NHFluentGenerator(applicationPreferences, table);
            castleGenerator = new CastleGenerator(applicationPreferences, table);
            contractGenerator = new ContractGenerator(applicationPreferences, table);
            byCodeGenerator = new ByCodeGenerator(applicationPreferences, table);
            if (applicationPreferences.ServerType == ServerType.Oracle)
            {
                mappingGenerator = new OracleMappingGenerator(applicationPreferences, table);
            }
            else
            {
                mappingGenerator = new SqlMappingGenerator(applicationPreferences, table);
            }
        }

        public void Generate()
        {
            //codeGenerator.Generate();
            if (applicationPreferences.IsNhFluent)
            {
                nhFluentGenerator.Generate();
            }
            else if (applicationPreferences.IsFluent) {
                var ccu = codeGenerator.GetCompileUnit(codeGenerator.ClassName);
                ccu = fluentGenerator.GetCompleteCompileUnit(ccu, fluentGenerator.ClassName);
                var generateCode = fluentGenerator.GenerateCode(ccu, fluentGenerator.ClassName);
                fluentGenerator.WriteToFile(generateCode, codeGenerator.ClassName);
            }
            else if (applicationPreferences.IsCastle)
            {
                castleGenerator.Generate();
            }
            else if (applicationPreferences.IsByCode)
            {
                byCodeGenerator.Generate();
            }
            else
            {
                mappingGenerator.Generate();
            }
            if(applicationPreferences.GenerateWcfDataContract)
            {
                contractGenerator.Generate();
            }
        }
    }
}