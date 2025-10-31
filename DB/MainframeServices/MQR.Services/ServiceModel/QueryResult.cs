using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace MQR.WebAPI.ServiceModel;

public class QueryResult : IXmlSerializable
{
   public QueryResultSection[] QueryResultSections { set; get; } = [];
      
   public DateTime MFReponseTime { get; set; } 
      
   public XmlSchema GetSchema()
   {
      return null;
   }

   public void ReadXml(XmlReader reader)
   {
      var sections = new List<QueryResultSection>();
      MFReponseTime = Convert.ToDateTime(reader.GetAttribute("MFReponseTime"));
      reader.Read();
         
      while (Operators.CompareString(reader.Name, "QueryResultSection", false) == 0 & reader.NodeType == XmlNodeType.Element)
      {
         var rows = new List<QueryResultRow>();
         var newQueryResultSection = new QueryResultSection
         {
            Identifier = reader.GetAttribute("Identifier")
         };
         reader.Read();
            
         while (Operators.CompareString(reader.Name, "QueryResultRow", false) == 0 & reader.NodeType == XmlNodeType.Element)
         {
            var fields = new List<QueryResultField>();
            var newQueryResultRow = new QueryResultRow
            {
               Identifier = reader.GetAttribute("Identifier"),
               FullLine = reader.GetAttribute("FullLine")
            };
            reader.Read();
               
            while (Operators.CompareString(reader.Name, "QueryResultField", false) == 0 & reader.NodeType == XmlNodeType.Element)
            {
               var newQueryResultField = new QueryResultField
               {
                  Identifier = reader.GetAttribute("Identifier"),
                  Value = reader.GetAttribute("Value")
               };
               reader.Read();
               
               fields.Add(newQueryResultField);
            }
            
            newQueryResultRow.Fields = fields.ToArray();
            rows.Add(newQueryResultRow);     
         }
            
         newQueryResultSection.Rows = rows.ToArray();
         sections.Add(newQueryResultSection);
      }
         
      QueryResultSections = sections.ToArray();
   }

   public void WriteXml(XmlWriter writer)
   {
      writer.WriteAttributeString("MFReponseTime", MFReponseTime.ToString("yyyy-MM-ddTHH:mm:ssz"));
         
      writer.WriteStartElement("QueryResultSections");   
      foreach (var section in QueryResultSections)
      {
         writer.WriteStartElement("QueryResultSection");
         writer.WriteAttributeString("Identifier", section.Identifier);
         foreach (var row in section.Rows)
         {
            writer.WriteStartElement("QueryResultRow");
            writer.WriteAttributeString("Identifier", row.Identifier);
            writer.WriteAttributeString("FullLine", row.FullLine);
            foreach (var field in row.Fields)
            {
               writer.WriteStartElement("QueryResultField");
               writer.WriteAttributeString("Identifier", field.Identifier);
               writer.WriteAttributeString("Value", field.Value);
               writer.WriteEndElement();
            }
            writer.WriteEndElement();
         }
         writer.WriteEndElement();
      }
      writer.WriteEndElement();   
   }
}