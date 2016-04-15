﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Bot Framework: http://botframework.com
// 
// Bot Builder SDK Github:
// https://github.com/Microsoft/BotBuilder
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;

namespace Microsoft.Bot.Builder.FormFlow.Advanced
{
    #region Documentation
    /// <summary>   Interface for localizing string resources. </summary>
    #endregion
    public interface ILocalizer
    {
        /// <summary>
        /// Return the localizer culture.
        /// </summary>
        /// <returns>Current culture.</returns>
        CultureInfo Culture { get; set; }

        /// <summary>
        /// Add a key and its translation.
        /// </summary>
        /// <param name="key">Key for indexing translation.</param>
        /// <param name="translation">Translation for key.</param>
        void Add(string key, string translation);

        /// <summary>
        /// Add a key and a list of translations.
        /// </summary>
        /// <param name="key">Key for indexing translation list.</param>
        /// <param name="list">List of translated terms.</param>
        void Add(string key, IEnumerable<string> list);

        #region Documentation
        /// <summary>   Adds values from dictionary under prefix;object. </summary>
        /// <param name="prefix">       The resource prefix. </param>
        /// <param name="dictionary">   The dictionary to add. </param>
        #endregion
        void Add(string prefix, IReadOnlyDictionary<object, string> dictionary);

        #region Documentation
        /// <summary>   Adds values from dictionary under prefix;object. </summary>
        /// <param name="prefix">       The resource prefix. </param>
        /// <param name="dictionary">   The dictionary to add. </param>
        #endregion
        void Add(string prefix, IReadOnlyDictionary<object, string[]> dictionary);

        #region Documentation
        /// <summary>   Adds patterns from template under prefix;usage;[fields with template]. </summary>
        /// <param name="prefix">       The resource prefix. </param>
        /// <param name="templates">    The template dictionary to add. </param>
        #endregion
        void Add(string prefix, IReadOnlyDictionary<TemplateUsage, TemplateAttribute> templates);

        #region Documentation
        /// <summary>   Adds patterns from template under prefix;usage;[fields with template]. </summary>
        /// <param name="prefix">       The resource prefix. </param>
        /// <param name="template">     The template to add. </param>
        #endregion
        void Add(string prefix, TemplateAttribute template);

        /// <summary>
        /// Translate a key to a translation.
        /// </summary>
        /// <param name="key">Key to lookup.</param>
        /// <param name="value">Value to set if present.</param>
        /// <returns>True if value is found. </returns>
        bool Lookup(string key, out string value);

        /// <summary>
        /// Translate a key to an array of values.
        /// </summary>
        /// <param name="key">Key to lookup.</param>
        /// <param name="values">Array value to set if present.</param>
        /// <returns>True if value is found. </returns>
        bool LookupValues(string key, out string[] values);

        #region Documentation
        /// <summary>   Look up prefix;object from dictionary and replace value from localizer. </summary>
        /// <param name="prefix">       The prefix. </param>
        /// <param name="dictionary">   Dictionary with existing values. </param>
        #endregion
        void LookupDictionary(string prefix, IDictionary<object, string> dictionary);

        #region Documentation
        /// <summary>   Look up prefix;object from dictionary and replace values from localizer. </summary>
        /// <param name="prefix">       The prefix. </param>
        /// <param name="dictionary">   Dictionary with existing values. </param>
        #endregion
        void LookupDictionary(string prefix, IDictionary<object, string[]> dictionary);

        #region Documentation
        /// <summary>   Looks up prefix;usage and replace patterns in template from localizer. </summary>
        /// <param name="prefix">       The prefix. </param>
        /// <param name="templates">    Template dictionary with existing values. </param>
        #endregion
        void LookupTemplates(string prefix, IDictionary<TemplateUsage, TemplateAttribute> templates);

        /// <summary>
        /// Remove a key from the localizer.
        /// </summary>
        /// <param name="key">Key to remove.</param>
        void Remove(string key);

        /// <summary>
        /// Save localizer resources to stream.
        /// </summary>
        /// <param name="stream">Where to write resources.</param>
        void Save(Stream stream);

        /// <summary>
        /// Load the localizer from a stream.
        /// </summary>
        /// <param name="stream">Stream to load from.</param>
        /// <param name="missing">Keys found in current localizer that are not in loaded localizer.</param>
        /// <param name="extra">Keys found in loaded localizer that were not in current localizer.</param>
        /// <returns>New localizer from stream.</returns>
        ILocalizer Load(Stream stream, out IEnumerable<string> missing, out IEnumerable<string> extra);
    }

    internal class ResourceLocalizer : ILocalizer
    {
        public CultureInfo Culture { get; set; }

        public void Add(string key, string translation)
        {
            _translations.Add(key, translation);
        }

        public void Add(string key, IEnumerable<string> list)
        {
            _arrayTranslations.Add(key, list.ToArray());
        }

        public void Add(string prefix, IReadOnlyDictionary<object, string> dictionary)
        {
            foreach (var entry in dictionary)
            {
                _translations.Add(prefix + SSEPERATOR + entry.Key, entry.Value);
            }
        }

        public void Add(string prefix, IReadOnlyDictionary<object, string[]> dictionary)
        {
            foreach (var entry in dictionary)
            {
                _arrayTranslations.Add(prefix + SSEPERATOR + entry.Key, entry.Value);
            }
        }

        public void Add(string prefix, IReadOnlyDictionary<TemplateUsage, TemplateAttribute> templates)
        {
            foreach (var template in templates.Values)
            {
                _templateTranslations.Add(MakeList(prefix, template.Usage.ToString()), template.Patterns);
            }
        }

        public void Add(string prefix, TemplateAttribute template)
        {
            _templateTranslations.Add(MakeList(prefix, template.Usage.ToString()), template.Patterns);
        }

        public bool Lookup(string key, out string value)
        {
            return _translations.TryGetValue(key, out value);
        }

        public bool LookupValues(string key, out string[] values)
        {
            return _arrayTranslations.TryGetValue(key, out values);
        }

        public void LookupDictionary(string prefix, IDictionary<object, string> dictionary)
        {
            foreach (var key in dictionary.Keys.ToArray())
            {
                string value;
                if (_translations.TryGetValue(prefix + SSEPERATOR + key, out value))
                {
                    dictionary[key] = value;
                }
            }
        }

        public void LookupDictionary(string prefix, IDictionary<object, string[]> dictionary)
        {
            foreach (var key in dictionary.Keys.ToArray())
            {
                string[] values;
                if (_arrayTranslations.TryGetValue(prefix + SSEPERATOR + key, out values))
                {
                    dictionary[key] = values;
                }
            }
        }

        public void LookupTemplates(string prefix, IDictionary<TemplateUsage, TemplateAttribute> templates)
        {
            foreach (var template in templates.Values)
            {
                string[] patterns;
                if (_templateTranslations.TryGetValue(prefix + SSEPERATOR + template.Usage, out patterns))
                {
                    template.Patterns = patterns;
                }
            }
        }

        public void Remove(string key)
        {
            _translations.Remove(key);
            _arrayTranslations.Remove(key);
            _templateTranslations.Remove(key);
        }

        public ILocalizer Load(Stream stream, out IEnumerable<string> missing, out IEnumerable<string> extra)
        {
            var lmissing = new List<string>();
            var lextra = new List<string>();
            var newLocalizer = new ResourceLocalizer();
            using (var reader = new ResourceReader(stream))
            {
                foreach (DictionaryEntry entry in reader)
                {
                    var fullKey = (string)entry.Key;
                    var semi = fullKey.IndexOf(SEPERATOR);
                    var type = fullKey.Substring(0, semi);
                    var key = fullKey.Substring(semi + 1);
                    var val = (string)entry.Value;
                    if (type == "CULTURE")
                    {
                        newLocalizer.Culture = new CultureInfo(val);
                    }
                    else if (type == "VALUE")
                    {
                        newLocalizer.Add(key, val);
                    }
                    else if (type == "LIST")
                    {
                        newLocalizer.Add(key, SplitList(val).ToArray());
                    }
                    else if (type == "TEMPLATE")
                    {
                        var elements = SplitList(key);
                        var usage = elements.First();
                        var fields = elements.Skip(1);
                        var patterns = SplitList(val);
                        var template = new TemplateAttribute((TemplateUsage)Enum.Parse(typeof(TemplateUsage), usage), patterns.ToArray());
                        foreach (var field in fields)
                        {
                            newLocalizer.Add(field, template);
                        }
                    }
                }
            }

            // Find missing and extra keys
            lmissing.AddRange(_translations.Keys.Except(newLocalizer._translations.Keys));
            lmissing.AddRange(_arrayTranslations.Keys.Except(newLocalizer._arrayTranslations.Keys));
            lmissing.AddRange(_templateTranslations.Keys.Except(newLocalizer._templateTranslations.Keys));
            lextra.AddRange(newLocalizer._translations.Keys.Except(_translations.Keys));
            lextra.AddRange(newLocalizer._arrayTranslations.Keys.Except(_arrayTranslations.Keys));
            lextra.AddRange(newLocalizer._templateTranslations.Keys.Except(_templateTranslations.Keys));
            missing = lmissing;
            extra = lextra;
            return newLocalizer;
        }

        public void Save(Stream stream)
        {
            using (var writer = new ResourceWriter(stream))
            {
                writer.AddResource("CULTURE" + SSEPERATOR, Culture.Name);
                foreach (var entry in _translations)
                {
                    writer.AddResource("VALUE" + SSEPERATOR + entry.Key, entry.Value);
                }

                foreach (var entry in _arrayTranslations)
                {
                    writer.AddResource("LIST" + SSEPERATOR + entry.Key, MakeList(entry.Value));
                }

                // Switch from field;usage -> patterns
                // to usage;pattern* -> [fields]
                var byPattern = new Dictionary<string, List<string>>();
                foreach (var entry in _templateTranslations)
                {
                    var names = SplitList(entry.Key).ToArray();
                    var field = names[0];
                    var usage = names[1];
                    var key = MakeList(AddPrefix(usage, entry.Value));
                    List<string> fields;
                    if (byPattern.TryGetValue(key, out fields))
                    {
                        fields.Add(field);
                    }
                    else
                    {
                        byPattern.Add(key, new List<string> { field });
                    }
                }

                // Write out TEMPLATE;usage;field* -> pattern*
                foreach (var entry in byPattern)
                {
                    var elements = SplitList(entry.Key).ToArray();
                    var usage = elements[0];
                    var patterns = elements.Skip(1);
                    var key = "TEMPLATE" + SSEPERATOR + usage + SSEPERATOR + MakeList(entry.Value);
                    writer.AddResource(key, MakeList(patterns));
                }
            }
        }

        protected const char SEPERATOR = ';';
        protected const string SSEPERATOR = ";";
        protected const string ESCAPED_SEPERATOR = "__semi";

        protected IEnumerable<string> AddPrefix(string prefix, IEnumerable<string> suffix)
        {
            return new string[] { prefix }.Union(suffix);
        }

        protected string MakeList(IEnumerable<string> elements)
        {
            return string.Join(SSEPERATOR, from elt in elements select elt.Replace(SSEPERATOR, ESCAPED_SEPERATOR));
        }

        protected string MakeList(params string[] elements)
        {
            return MakeList(elements.AsEnumerable<string>());
        }

        protected IEnumerable<string> SplitList(string str)
        {
            var elements = str.Split(SEPERATOR);
            return from elt in elements select elt.Replace(ESCAPED_SEPERATOR, SSEPERATOR);
        }

        protected Dictionary<string, string> _translations = new Dictionary<string, string>();
        protected Dictionary<string, string[]> _arrayTranslations = new Dictionary<string, string[]>();
        protected Dictionary<string, string[]> _templateTranslations = new Dictionary<string, string[]>();
    }
}