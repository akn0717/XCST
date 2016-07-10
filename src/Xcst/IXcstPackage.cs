﻿// Copyright 2015 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Xcst.Runtime;

namespace Xcst {

   public interface IXcstPackage {

      ExecutionContext Context { get; set; }

      void Prime(PrimingContext context);

      void CallTemplate(QualifiedName name, DynamicContext context);

      void ReadOutputDefinition(QualifiedName name, OutputParameters parameters);
   }

   namespace Packages {

      public abstract class XcstComponentAttribute : Attribute {

         public string Name { get; set; }
      }

      [AttributeUsage(AttributeTargets.Method)]
      public class XcstAttributeSetAttribute : XcstComponentAttribute { }

      [AttributeUsage(AttributeTargets.Method)]
      public class XcstFunctionAttribute : XcstComponentAttribute { }

      [AttributeUsage(AttributeTargets.Property)]
      public class XcstParameterAttribute : XcstComponentAttribute { }

      [AttributeUsage(AttributeTargets.Method)]
      public class XcstTemplateAttribute : XcstComponentAttribute { }

      [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
      public class XcstTemplateParameterAttribute : Attribute {

         public string Name { get; }

         public bool Required { get; set; }

         public bool Tunnel { get; set; }

         public XcstTemplateParameterAttribute(string name) {
            this.Name = name;
         }
      }

      [AttributeUsage(AttributeTargets.Class)]
      public class XcstTypeAttribute : XcstComponentAttribute { }

      [AttributeUsage(AttributeTargets.Property)]
      public class XcstVariableAttribute : XcstComponentAttribute { }
   }
}