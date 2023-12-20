using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Common
{
   public class TraceBaggageEnricher : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity data)
        {
            var baggageDictionary = Baggage.GetBaggage();
            foreach (var baggage in baggageDictionary)
            {
                Debug.WriteLine($"{Process.GetCurrentProcess().ProcessName} ENRICHING via Baggage.GetBaggage {baggage.Key}:{baggage.Value}");
                data.SetTag(baggage.Key, baggage.Value);
            }

            foreach(var baggage in data.Baggage)
            {
                Debug.WriteLine($"{Process.GetCurrentProcess().ProcessName} ENRICHING via Activity.Baggage {baggage.Key}:{baggage.Value}");
                data.SetTag(baggage.Key, baggage.Value);
            }
        }
    }

}
