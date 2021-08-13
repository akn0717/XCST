﻿// Copyright 2020 Max Toro Q.
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

using System.Diagnostics;
using System.Xml.Linq;
using Xcst.Runtime;

namespace Xcst.Xml {

   // XNodeBuilder for XElement does not support top level attributes.
   // The workaround is to create a writer for a document,
   // then detaching the root element from that document.
   // 
   // The generated code for c:element and literal result elements do not dispose/close writers.
   // Therefore, using WriteEndElement to check when element is ready,
   // then calling Close on baseWriter.

   class XElementWriter : WrappingXmlWriter {

      readonly XDocument
      _document;

      readonly ISequenceWriter<XElement>
      _output;

      int
      _depth;

      bool
      _elementFlushed;

      public
      XElementWriter(XDocument document, ISequenceWriter<XElement> output)
         : base(document.CreateWriter()) {

         _document = document;
         _output = output;
      }

      public override void
      WriteStartElement(string? prefix, string localName, string? ns) {

         base.WriteStartElement(prefix, localName, ns);

         _depth++;
      }

      public override void
      WriteEndElement() {

         base.WriteEndElement();

         _depth--;

         if (_depth == 0) {
            Close();
         }
      }

      public override void
      Close() {

         Debug.Assert(!_elementFlushed);

         if (!_elementFlushed) {

            base.Close();

            XElement el = _document.Root!;

            Assert.That(el != null);

            el.Remove();

            _output.WriteObject(el);

            _elementFlushed = true;
         }
      }
   }
}
