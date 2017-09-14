﻿// Copyright 2017 Max Toro Q.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using Xcst.PackageModel;

namespace Xcst.Runtime {

   using IExpandoMap = IDictionary<string, object>;
   using ExpandoArray = List<object>;

   class ExpandoEntry {

      public readonly string Key;

      public ExpandoEntry(string key) {
         this.Key = key;
      }
   }

   public class ExpandoMapWriter : MapWriter {

      readonly ISequenceWriter<ExpandoObject> mapOutput;
      readonly ISequenceWriter<object> arrayOutput;
      readonly List<object> objects = new List<object>();

      public ExpandoMapWriter(ISequenceWriter<ExpandoObject> output) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Debug.Assert(output.TryCastToDocumentWriter() == null);

         this.mapOutput = output;
      }

      public ExpandoMapWriter(ISequenceWriter<object> output) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         Debug.Assert(output.TryCastToDocumentWriter() == null);

         this.arrayOutput = output;
      }

      public override void WriteStartMap() {

         var map = new ExpandoObject();

         if (this.objects.Count == 0) {

            this.mapOutput.WriteObject(map);
            Push(map);
            return;
         }

         var parent = Peek<object>();
         var parentArr = parent as ExpandoArray;

         if (parentArr != null) {
            parentArr.Add(map);
            Push(map);
            return;
         }

         var entry = parent as ExpandoEntry;

         if (entry != null) {
            SetEntryValue(entry, map);
            Push(map);
            return;
         }

         throw new RuntimeException("A map can only be written to an entry or array.");
      }

      public override void WriteEndMap() {
         Pop();
      }

      public override void WriteStartArray() {

         var arr = new ExpandoArray();
         object parent;

         if (this.objects.Count == 0
            || (parent = Peek<object>()) is ExpandoEntry
            || parent is ExpandoArray) {

            // Arrays are buffered and added to parent object on end call

            Push(arr);
            return;
         }

         throw new RuntimeException("An array can only be written to an entry or another array.");
      }

      public override void WriteEndArray() {

         var array = Peek<ExpandoArray>();

         WriteEndArray(array);
      }

      void WriteEndArray(ExpandoArray array) {

         Debug.Assert(array != null);

         object[] items = array.ToArray();

         array.Clear();

         // SetEntryValue call below may Push
         // Must Pop before that
         Pop();

         if (this.objects.Count == 0) {

            // Cast to object to avoid flattening

            this.arrayOutput.WriteObject((object)items);

         } else {

            var parent = Peek<object>();
            var entry = parent as ExpandoEntry;

            if (entry != null) {

               SetEntryValue(entry, items);

            } else {

               var parentArray = parent as ExpandoArray;

               Debug.Assert(parentArray != null);

               parentArray.Add(items);
            }
         }
      }

      public override void WriteStartMapEntry(string key) {

         if (key == null) throw new ArgumentNullException(nameof(key));

         var map = Peek<IExpandoMap>();

         if (map == null) {
            throw new RuntimeException("An entry can only be written to a map.");
         }

         Push(new ExpandoEntry(key));
      }

      public override void WriteEndMapEntry() {

         var parent = Peek<object>();

         var entry = parent as ExpandoEntry;

         if (entry != null) {

            var map = Peek<IExpandoMap>(1);

            Debug.Assert(map != null);

            if (!map.ContainsKey(entry.Key)) {
               // No value written, write null
               map[entry.Key] = null;
            }

         } else {

            var implicitArray = parent as ExpandoArray;

            Debug.Assert(implicitArray != null);

            WriteEndArray(implicitArray);

            entry = Peek<ExpandoEntry>();

            Debug.Assert(entry != null);
         }

         Pop();
      }

      public override void WriteComment(string text) { }

      #region ISequenceWriter<object> Members

      public override void WriteObject(object value) {

         var parent = Peek<object>();

         var arr = parent as ExpandoArray;

         if (arr != null) {
            arr.Add(value);
            return;
         }

         var entry = parent as ExpandoEntry;

         if (entry != null) {
            SetEntryValue(entry, value);
            return;
         }

         throw new RuntimeException("A value can only be written to an entry or array.");
      }

      public override void WriteString(object text) {
         WriteObject((string)text);
      }

      public override void WriteRaw(object data) {
         throw new NotImplementedException();
      }

      public override XcstWriter TryCastToDocumentWriter() {
         return null;
      }

      #endregion

      void Push(object obj) {
         this.objects.Add(obj);
      }

      T/*?*/ Peek<T>(int offset = 0) where T : class {

         int index = this.objects.Count - 1 - offset;

         Debug.Assert(index >= 0);

         return this.objects[index] as T;
      }

      void Pop() {

         Debug.Assert(this.objects.Count > 0);

         this.objects.RemoveAt(this.objects.Count - 1);
      }

      void SetEntryValue(ExpandoEntry entry, object value) {

         var map = Peek<IExpandoMap>(1);

         Debug.Assert(map != null);

         if (!map.ContainsKey(entry.Key)) {
            map[entry.Key] = value;
         } else {

            object existingValue = map[entry.Key];
            var implicitArray = new ExpandoArray { existingValue, value };

            Push(implicitArray);

            map.Remove(entry.Key);
         }
      }
   }
}
