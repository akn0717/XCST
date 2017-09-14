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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Xcst.PackageModel;
using Xcst.Runtime;

namespace Xcst {

   public abstract class XcstWriter : ISequenceWriter<object>, IDisposable {

      bool disposed;

      public virtual Uri OutputUri { get; }

      /// <exclude/>

      [EditorBrowsable(EditorBrowsableState.Never)]
      public virtual SimpleContent SimpleContent { get; internal set; }

      protected XcstWriter(Uri outputUri) {

         if (outputUri == null) throw new ArgumentNullException(nameof(outputUri));

         this.OutputUri = outputUri;
      }

      public void WriteStartElement(string localName) {
         WriteStartElement(null, localName, default(string));
      }

      public void WriteStartElement(string localName, string ns) {
         WriteStartElement(null, localName, ns);
      }

      public abstract void WriteStartElement(string prefix, string localName, string ns);

      public abstract void WriteEndElement();

      /// <exclude/>

      [EditorBrowsable(EditorBrowsableState.Never)]
      public void WriteStartElementLexical(string lexical, string ns, string defaultNs) {

         int prefixIndex = lexical.IndexOf(':');
         bool hasPrefix = prefixIndex > 0;

         string prefix = (hasPrefix) ? lexical.Substring(0, prefixIndex) : null;
         string localName = (hasPrefix) ? lexical.Substring(prefixIndex + 1) : lexical;

         if (hasPrefix) {

            if (String.IsNullOrEmpty(ns)) {
               throw new NotSupportedException();
            }

            WriteStartElement(prefix, localName, ns);

         } else {
            WriteStartElement(null, localName, ns ?? defaultNs);
         }
      }

      public void WriteAttributeString(string localName, string ns, string value) {

         WriteStartAttribute(null, localName, ns);
         WriteString(value);
         WriteEndAttribute();
      }

      public void WriteAttributeString(string localName, string value) {

         WriteStartAttribute(null, localName, default(string));
         WriteString(value);
         WriteEndAttribute();
      }

      public void WriteAttributeString(string prefix, string localName, string ns, string value) {

         WriteStartAttribute(prefix, localName, ns);
         WriteString(value);
         WriteEndAttribute();
      }

      public void WriteStartAttribute(string localName) {
         WriteStartAttribute(null, localName, default(string), default(string));
      }

      public void WriteStartAttribute(string localName, string ns) {
         WriteStartAttribute(null, localName, ns, default(string));
      }

      public void WriteStartAttribute(string prefix, string localName, string ns) {
         WriteStartAttribute(prefix, localName, ns, default(string));
      }

      public abstract void WriteStartAttribute(string prefix, string localName, string ns, string separator);

      public abstract void WriteEndAttribute();

      /// <exclude/>

      [EditorBrowsable(EditorBrowsableState.Never)]
      public void WriteAttributeStringLexical(string lexical, string ns, string value) {

         WriteStartAttributeLexical(lexical, ns, null);
         WriteString(value);
         WriteEndAttribute();
      }

      /// <exclude/>

      [EditorBrowsable(EditorBrowsableState.Never)]
      public void WriteStartAttributeLexical(string lexical, string ns) {
         WriteStartAttributeLexical(lexical, ns, null);
      }

      /// <exclude/>

      [EditorBrowsable(EditorBrowsableState.Never)]
      public void WriteStartAttributeLexical(string lexical, string ns, string separator) {

         int prefixIndex = lexical.IndexOf(':');
         bool hasPrefix = prefixIndex > 0;

         string prefix = (hasPrefix) ? lexical.Substring(0, prefixIndex) : null;
         string localName = (hasPrefix) ? lexical.Substring(prefixIndex + 1) : lexical;

         if (hasPrefix
            && String.IsNullOrEmpty(ns)) {

            throw new NotSupportedException();
         }

         WriteStartAttribute(prefix, localName, ns, separator);
      }

      public abstract void WriteProcessingInstruction(string name, string text);

      public abstract void WriteComment(string text);

      public abstract void WriteString(string text);

      public abstract void WriteChars(char[] buffer, int index, int count);

      public abstract void WriteRaw(string data);

      public abstract void Flush();

      public void Dispose() {

         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing) {

         if (this.disposed) {
            return;
         }

         this.disposed = true;
      }

      #region ISequenceWriter<object> Members

      public void WriteString(object text) {
         WriteString(this.SimpleContent.Convert(text));
      }

      public void WriteRaw(object data) {
         WriteRaw(this.SimpleContent.Convert(data));
      }

      public virtual void WriteObject(object value) {

         if (value != null) {
            WriteString(value);
         }
      }

      public void WriteObject(IEnumerable<object> value) {

         if (value != null) {

            foreach (var item in value) {
               WriteObject(item);
            }
         }
      }

      public XcstWriter TryCastToDocumentWriter() {
         return this;
      }

      public MapWriter TryCastToMapWriter() {
         return null;
      }

      #endregion

      // IEnumerable<object> works for reference types only
      // IEnumerable for any type

      public void WriteObject(IEnumerable value) {

         if (value != null) {

            foreach (var item in value) {
               WriteObject(item);
            }
         }
      }

      // string implements IEnumerable, treat as single value

      public void WriteObject(string value) {
         WriteObject((object)value);
      }
   }
}
