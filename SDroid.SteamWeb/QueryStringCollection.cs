using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;

namespace SDroid.SteamWeb
{
    public class QueryStringBuilder : List<KeyValuePair<string, object>>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="QueryStringBuilder" /> class.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        public QueryStringBuilder(IEnumerable<KeyValuePair<string, object>> collection) : base(collection)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="QueryStringBuilder" /> class.
        /// </summary>
        public QueryStringBuilder()
        {
        }

        public static QueryStringBuilder FromDynamic(dynamic obj)
        {
            if (obj == null)
            {
                return new QueryStringBuilder();
            }

            return new QueryStringBuilder(
                (TypeDescriptor.GetProperties(obj) as PropertyDescriptorCollection)
                ?.Cast<PropertyDescriptor>()
                .ToDictionary(descriptor => descriptor.Name, descriptor => descriptor.GetValue(obj)));
        }

        public static QueryStringBuilder FromObject(object obj)
        {
            return FromDynamic(obj);
        }

        /// <summary>
        ///     Returns the values saved in this instance as a Url compatible and encoded string.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance's saved values.
        /// </returns>
        public override string ToString()
        {
            return string.Join(
                "&",
                this.Select(
                    kvp => string.Concat(
                        Uri.EscapeDataString(kvp.Key),
                        "=",
                        Uri.EscapeDataString(ValueToString(kvp.Value))
                    )
                )
            );
        }

        /// <summary>
        ///     Adds a new value to this instance.
        /// </summary>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The actual value.</param>
        // ReSharper disable once MethodNameNotMeaningful
        public void Add(string name, object value)
        {
            Add(new KeyValuePair<string, object>(name, value));
        }

        /// <summary>
        ///     Appends this instance to an URL as query string.
        /// </summary>
        /// <param name="baseUrl">The base URL to append.</param>
        /// <returns>The new URL containing the query string representing the values saved in this instance.</returns>
        public string AppendToUrl(string baseUrl)
        {
            return baseUrl + (baseUrl.Contains("?") ? "&" : "?") + ToString();
        }

        public MultipartFormDataContent ToMultipartFormDataContent()
        {
            var form = new MultipartFormDataContent();

            foreach (var part in this)
            {
                if (part.Value is bool boolVal)
                {
                    form.Add(new StringContent((boolVal ? 1 : 0).ToString(), Encoding.UTF8), part.Key);
                } else if (part.Value is byte[] byteArray) {
                    form.Add(new ByteArrayContent(byteArray), part.Key);
                } else {
                    form.Add(new StringContent(part.Value.ToString(), Encoding.UTF8), part.Key);
                }
            }

            return form;
        }

        /// <summary>
        ///     Creates a new instance containing the values of this instance and a new instance.
        /// </summary>
        /// <param name="collection">The second collection.</param>
        /// <returns>A new instance containing values of both collections</returns>
        public QueryStringBuilder Concat(IEnumerable<KeyValuePair<string, object>> collection)
        {
            return new QueryStringBuilder(ToArray().Concat(collection));
        }

        private string ValueToString(object value)
        {
            if (value is bool boolVal)
            {
                return (boolVal ? 1 : 0).ToString();
            }

            if (value is byte[] byteArray)
            {
                return HttpUtility.UrlEncode(byteArray);
            }

            return value?.ToString() ?? "";
        }
    }
}