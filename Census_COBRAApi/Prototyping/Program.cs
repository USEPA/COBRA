using System;
using System.Collections.Generic;
using CobraCompute;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;

namespace Prototyping
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");

          
            CobraComputeCore core = new CobraComputeCore();
            core.initialize("C:\\Users\\developer\\Projects\\COBRA\\COBRA.API\\COBRA.NET.API\\testdata\\");

            Guid token = core.createUserScenario();

            List<EmissionsRecord> data = core.GetBaseEmissions("typeindx=2");

            List<EmissionsRecord> AllEmissionsData = core.GetControlEmissions(token, "");

              

            //tyhis should turn Maryland Highway PM off
            foreach (EmissionsRecord rec in AllEmissionsData)
            {
                if ((rec.stid == 24) && (rec.TIER1 == 11))
                {
                    rec.PM25 = 0;
                    core.SetControlEmissions(token, new EmissionsRecord[] { rec });
                }
            }

            //List<EmissionsRecord> data3 = core.GetSummarizedControlEmissionsbyTier(token, "stid=21");

            core.ComputeDeltaPM(token);

            Console.WriteLine("Done!");
            
        }
    }
}
