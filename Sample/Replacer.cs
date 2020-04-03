using Cake.Core;
using Cake.Core.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using LogLevel = Cake.Core.Diagnostics.LogLevel;
using Verbosity = Cake.Core.Diagnostics.Verbosity;


namespace Sample
{
    public static class Replacer
    {
        [CakeMethodAlias]
        public static void ReplaceAppSetting(this ICakeContext context, string filename, string key, string newValue)
        {
            var xml = new XmlDocument();
            xml.Load(filename);

            var node = xml.SelectSingleNode($"/configuration/appSettings/add[@key='{key}']");
            if (node == null)
            {
                throw new InvalidOperationException($"ApplicationSetting Key ({key}) does not exist.");
            }

            var valueAttribute = node.Attributes["value"];
            if (valueAttribute == null)
            {
                valueAttribute = xml.CreateAttribute("value");
                node.Attributes.SetNamedItem(valueAttribute);
            }
            valueAttribute.Value = newValue;

            xml.Save(filename);

            context?.Log.Write(Verbosity.Normal, LogLevel.Debug, $"Replacing {filename} appSetting key={key} with value={newValue}");
        }
    }
}
