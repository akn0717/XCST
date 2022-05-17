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
using System.Collections.Generic;
using System.Linq;

namespace Xcst.Runtime;

/// <exclude/>
public static class Sorting {

   public static IOrderedEnumerable<TSource>
   SortBy<TSource, TKey>(
         IEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool descending = false) {

      if (descending) {
         return Enumerable.OrderByDescending(source, keySelector);
      }

      return Enumerable.OrderBy(source, keySelector);
   }
}
