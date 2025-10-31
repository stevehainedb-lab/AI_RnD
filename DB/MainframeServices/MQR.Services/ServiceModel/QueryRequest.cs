using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MQR.WebAPI.ServiceModel
{
   //[DataContract(Namespace = "")]
   public class QueryRequest : IXmlSerializable
   {
      public Guid RequestId { get; set; } = Guid.NewGuid();

      public QueryRequestParameter[] Parameters { get; set; } = [];

      public string LogonInstructionSet { get; set; }

      public string QueryInstructionSet { get; set; }

      public string ParseInstructionSet { get; set; }

      public bool ExpectResponse { get; set; }

      public string CallingApplication { get; set; } = "UNKNOWN";

      public int TimeOutSeconds { get; set; }
      
      public string? ClientTrackingId { get; set; }
      
      public string? SessionUsed { get; set; }

      public XmlSchema GetSchema()
      {
         return null;
      }

      public void ReadXml(XmlReader reader)
      {
      }

      public void WriteXml(XmlWriter writer)
      {
         //writer.Settings.ConformanceLevel = ConformanceLevel.Fragment; 
         
         writer.WriteAttributeString("ID", RequestId.ToString());
         writer.WriteAttributeString("LogonInstructionset", LogonInstructionSet);
         writer.WriteAttributeString("QueryInstructionSet", QueryInstructionSet);
         writer.WriteAttributeString("ParseInstructionSet", ParseInstructionSet);
         writer.WriteAttributeString("ExpectResponse", ExpectResponse.ToString());
         writer.WriteAttributeString("TimeOutSeconds", TimeOutSeconds.ToString());
         writer.WriteAttributeString("CallingApplication", CallingApplication);

         //writer.WriteStartElement("QueryRequestParameters");   
         foreach (var param in Parameters)
         {
            writer.WriteStartElement("QueryRequestParameter");
            writer.WriteAttributeString("Identifier", param.Identifier);
            writer.WriteAttributeString("Value", param.Value);
            writer.WriteEndElement();
         }
         //writer.WriteEndElement();   
      }

      public override string ToString()
      {
         var paramString = string.Join(",", Parameters.Select(p => p.ToString()));
         return $"L:{LogonInstructionSet} Q:{QueryInstructionSet} P:{ParseInstructionSet} {paramString} - {RequestId}";
      }
   }
}