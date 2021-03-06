﻿/*
    Copyright (C) 2013 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.RestGenerator;

namespace Rhetos.RestGenerator.DefaultConcepts
{
    [Export(typeof(IRestGeneratorPlugin))]
    [ExportMetadata(MefProvider.Implements, typeof(ActionInfo))]
    public class ActionCodeGenerator : IRestGeneratorPlugin
    {
        private static string ServiceRegistrationCodeSnippet(ActionInfo info)
        {
            return string.Format(@"builder.RegisterType<RestService{0}{1}>().InstancePerLifetimeScope();
            ", info.Module.Name, info.Name);
        }

        private static string ServiceInitializationCodeSnippet(ActionInfo info)
        {
            return string.Format(@"System.Web.Routing.RouteTable.Routes.Add(new System.ServiceModel.Activation.ServiceRoute(""Rest/{0}/{1}"", 
                new RestServiceHostFactory(), typeof(RestService{0}{1})));
            ", info.Module.Name, info.Name);
        }
    
        private static string ServiceDefinitionCodeSnippet(ActionInfo info)
        {
            return String.Format(
@"
    [System.ServiceModel.ServiceContract]
    [System.ServiceModel.Activation.AspNetCompatibilityRequirements(RequirementsMode = System.ServiceModel.Activation.AspNetCompatibilityRequirementsMode.Allowed)]
    public class RestService{0}{1}
    {{
        private ServiceLoader _serviceLoader;

        public RestService{0}{1}(ServiceLoader serviceLoader) 
        {{
            _serviceLoader = serviceLoader;
        }}

        [OperationContract]
        [WebInvoke(Method = ""POST"", UriTemplate = """", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void Execute{0}{1}({0}.{1} action)
        {{
            _serviceLoader.Execute<{0}.{1}>(action);
        }}
    }}

", info.Module.Name, info.Name);
        }
        
        private static bool _isInitialCallMade;

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ActionInfo)conceptInfo;
            GenerateInitialCode(codeBuilder);

            codeBuilder.InsertCode(ServiceRegistrationCodeSnippet(info), InitialCodeGenerator.ServiceRegistrationTag);
            codeBuilder.InsertCode(ServiceInitializationCodeSnippet(info), InitialCodeGenerator.ServiceInitializationTag);
            codeBuilder.InsertCode(ServiceDefinitionCodeSnippet(info), InitialCodeGenerator.RhetosRestClassesTag);
        }

        private void GenerateInitialCode(ICodeBuilder codeBuilder)
        {
            if (_isInitialCallMade)
                return;
            _isInitialCallMade = true;
            codeBuilder.InsertCode(@"
        public void Execute<T>(T action)
        {
            var commandInfo = new ExecuteActionCommandInfo { Action = action };
            var result = _processingEngine.Execute(new[]{commandInfo});
            CheckForErrors(result);
        }
", InitialCodeGenerator.ServiceLoaderMembersTag);
        }
    }
}
