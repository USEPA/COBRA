using cobra_console.models;
using CsvHelper;
using NCalc;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace cobra_console.units
{
    class DestinationRecord
    {
        public double destindx { get; set; }
        public string endpoint { get; set; }
        public double value { get; set; }

    }

    public class ImpactProcessor
    {
        // used to keep track of time spent in crfunc()
        private static Stopwatch globalStopwatch = new Stopwatch();
        private long totalTicks = 0;
        //health functions
        /*
            1   (1-(1/((1-Incidence)*Exp(Beta*DELTAQ)+Incidence)))*Incidence*POP
            2   (1-(1/((1-Incidence)*Exp(Beta*DELTAQ)+Incidence)))*Incidence*A*POP
            3   (1-(1/Exp(Beta*DELTAQ)))*Incidence*POP*A
            4   (A - (A/((1-A)*Exp(Beta*DELTAQ)+A)))*POP*B
            5   (1-(1/((1-A)*Exp(Beta*DELTAQ)+A)))*A*POP*B
            6   (1-Exp(-Beta*DELTAQ))*Incidence*POP
            7   (Incidence - (Incidence/((1-Incidence)*Exp(Beta*DELTAQ)+Incidence)))*POP
            8   (1-(1/Exp(Beta*DELTAQ)))*Incidence*POP
            9   (1-(1/((1-A)*Exp(Beta*DELTAQ)+A)))*A*POP
            10  (1-(1/Exp(Beta*DELTAQ)))*A*POP
            11  (1 - (1 / Exp(Beta * DELTAQ))) * A * POP * Incidence
            12 (1-(1/Exp(Beta*DELTAQ)))*Incidence*POP*(1-A)
         */

        private double crfunc(string rawfunction, string compfunction, double Incidence, double Beta, double DELTAQ, double DELTAO, double POP, Double A, Double B, double C)
        {
            // stopwatch to measure current call's duration
            //Stopwatch stopwatch = Stopwatch.StartNew();
            double result = 0;
            switch (compfunction)
            {

                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*POP":
                    result = (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * POP;
                    break;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*A*POP": //this one from CR function is probablyt incorrect
                    result = (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * A * POP;
                    break;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*POP*A": //this one from CR function is probablyt incorrect
                    result = (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * A * POP;
                    break;
                case "(1-(1/((1-INCIDENCE*A)*EXP(BETA*DELTAQ)+INCIDENCE*A)))*INCIDENCE*A*POP": //this one from CR function is probablyt incorrect
                    result = (1 - (1 / ((1 - Incidence * A) * Math.Exp(Beta * DELTAQ) + Incidence * A))) * Incidence * A * POP;
                    break;
                case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*A*POP":
                    result = (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP * A;
                    break;
                case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*POP*A":
                    result = (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP * A;
                    break;
                case "(1-EXP(-BETA*DELTAQ))*INCIDENCE*POP*A":  //alternate notation
                    result = (1 - Math.Exp(-Beta * DELTAQ)) * Incidence * POP * A;
                    break;
                case "(1-EXP(-BETA*DELTAQ))*INCIDENCE*A*POP":  //alternate notation
                    result = (1 - Math.Exp(-Beta * DELTAQ)) * Incidence * POP * A;
                    break;
                case "(A-(A/((1-A)*EXP(BETA*DELTAQ)+A)))*POP*B":
                    result = (A - (A / ((1 - A) * Math.Exp(Beta * DELTAQ) + A))) * POP * B;
                    break;
                case "(1-(1/((1-A)*EXP(BETA*DELTAQ)+A)))*A*POP*B":
                    result = (1 - (1 / ((1 - A) * Math.Exp(Beta * DELTAQ) + A))) * A * POP * B;
                    break;
                case "(1-EXP(-BETA*DELTAQ))*INCIDENCE*POP":
                    result = (1 - Math.Exp(-Beta * DELTAQ)) * Incidence * POP;
                    break;
                case "(INCIDENCE-(INCIDENCE/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*POP":
                    result = (Incidence - (Incidence / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * POP;
                    break;
                case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*POP":
                    result = (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP;
                    break;
                case "(1-(1/((1-A)*EXP(BETA*DELTAQ)+A)))*A*POP":
                    result = (1 - (1 / ((1 - A) * Math.Exp(Beta * DELTAQ) + A))) * A * POP;
                    break;
                case "(1-(1/EXP(BETA*DELTAQ)))*A*POP":
                    result = (1 - (1 / Math.Exp(Beta * DELTAQ))) * A * POP;
                    break;
                case "(1-(1/EXP(BETA*DELTAQ)))*A*POP*INCIDENCE":
                    result = (1 - (1 / Math.Exp(Beta * DELTAQ))) * A * Incidence * POP;
                    break;
                case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*POP*(1-A)":
                    result = (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP * (1 - A);
                    break;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DeltaQ)+INCIDENCE)))*INCIDENCE*POP":
                    result = (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * POP;
                    break;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*A*DeltaQ)+INCIDENCE)))*INCIDENCE*POP":
                    result = (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * A * DELTAQ) + Incidence))) * Incidence * POP;
                    break;
                //DELTAO PARSING
                case "(1-(1/EXP(BETA*DELTAO)))*INCIDENCE*POP":
                    result = (1 - (1 / Math.Exp(Beta * DELTAO))) * Incidence * POP;
                    break;
                case "(1-(1/((1-A)*EXP(BETA*DELTAO)+A)))*A*POP*INCIDENCE":
                    result = (1 - (1 / ((1 - A) * Math.Exp(Beta * DELTAO) + A))) * A * POP * Incidence;
                    break;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*A*DeltaO)+INCIDENCE)))*INCIDENCE*POP":
                    result = (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * A * DELTAO) + Incidence))) * Incidence * POP;
                    break;
                case "(1-(1/EXP(BETA*DELTAO)))*INCIDENCE*A*POP*(1-A)":
                    result = (1 - (1 / Math.Exp(Beta * DELTAO))) * Incidence * POP * (1 - A);
                    break;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DeltaO)+INCIDENCE)))*INCIDENCE*POP":
                    result = (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAO) + Incidence))) * Incidence * POP;
                    break;
                case "(1-(1/EXP(BETA*DELTAO)))*INCIDENCE*POP*A*B":
                    result = (1 - (1 / Math.Exp(Beta * DELTAO))) * Incidence * POP * A * B;
                    break;
                default:
                    Expression e = new Expression(rawfunction);
                    e.Parameters["A"] = A;
                    e.Parameters["B"] = B;
                    e.Parameters["C"] = C;
                    e.Parameters["Beta"] = Beta;
                    e.Parameters["DELTAQ"] = DELTAQ;
                    e.Parameters["DELTAO"] = DELTAO;
                    e.Parameters["Incidence"] = Incidence;
                    e.Parameters["POP"] = POP;
                    result = (double)e.Evaluate();
                    break;
            }

            //stopwatch.Stop();
            // Accumulate the ticks from the current call
            //totalTicks += stopwatch.ElapsedTicks;

            //DisplayTotalElapsedTime();


            return result;
        }

        public void DisplayTotalElapsedTime()
        {
            // Use TimeSpan to convert ticks to a more readable format
            TimeSpan totalTimeSpent = TimeSpan.FromTicks(totalTicks);
            // Explicitly stating the total time in milliseconds in the log
            Console.WriteLine($"********************************************************* Total time spent in crfunc: {totalTimeSpent.TotalMilliseconds} milliseconds");
            Console.WriteLine($"Formatted: {totalTimeSpent.ToString("c")} (days.hours:minutes:seconds.milliseconds)");
        }

        public List<string> messages = new List<string>();

        private SQLiteConnection _dbConn;
        private string _EF_connectionstring;

        public ImpactProcessor(SQLiteConnection dbConn, string EF_connectionstring)
        {
            _dbConn = dbConn;
            _EF_connectionstring = EF_connectionstring;
        }

        private void executeQuery(string query)
        {
            SQLiteCommand cmd = _dbConn.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
        }

        private void clearTable(string tablename)
        {
            executeQuery("delete from " + tablename);
        }

        private void defaultauxtables(FileOrTable popdata, FileOrTable incidencedata, FileOrTable crdata, FileOrTable valdata)
        {
            SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
            dbConn.Open();
            Dataloader loader = new Dataloader(dbConn);

            executeQuery("delete from SYS_POP");
            executeQuery("delete from SYS_Incidence");
            executeQuery("delete from SYS_CR");
            executeQuery("delete from SYS_Valuation");

            if (popdata.usetable)
            {
                executeQuery("INSERT INTO [SYS_POP] SELECT NULL, Year, DestinationID, FIPS, Age0, Age1, Age2, Age3, Age4, Age5, Age6, Age7, Age8, Age9, Age10, Age11, Age12, Age13, Age14, Age15, Age16, Age17, Age18, Age19, Age20, Age21, Age22, Age23, Age24, Age25, Age26, Age27, Age28, Age29, Age30, Age31, Age32, Age33, Age34, Age35, Age36, Age37, Age38, Age39, Age40, Age41, Age42, Age43, Age44, Age45, Age46, Age47, Age48, Age49, Age50, Age51, Age52, Age53, Age54, Age55, Age56, Age57, Age58, Age59, Age60, Age61, Age62, Age63, Age64, Age65, Age66, Age67, Age68, Age69, Age70, Age71, Age72, Age73, Age74, Age75, Age76, Age77, Age78, Age79, Age80, Age81, Age82, Age83, Age84, Age85, Age86, Age87, Age88, Age89, Age90, Age91, Age92, Age93, Age94, Age95, Age96, Age97, Age98, Age99 FROM [SYS_POP_INVENTORY] where ID=" + popdata.dataidx.ToString());
            }
            else
            {
                loader.loadpopulationfile(popdata.filename);
            }
            if (incidencedata.usetable)
            {
                executeQuery("INSERT INTO [SYS_Incidence] SELECT NULL, Year, DestinationID, FIPS, Endpoint, Age0, Age1, Age2, Age3, Age4, Age5, Age6, Age7, Age8, Age9, Age10, Age11, Age12, Age13, Age14, Age15, Age16, Age17, Age18, Age19, Age20, Age21, Age22, Age23, Age24, Age25, Age26, Age27, Age28, Age29, Age30, Age31, Age32, Age33, Age34, Age35, Age36, Age37, Age38, Age39, Age40, Age41, Age42, Age43, Age44, Age45, Age46, Age47, Age48, Age49, Age50, Age51, Age52, Age53, Age54, Age55, Age56, Age57, Age58, Age59, Age60, Age61, Age62, Age63, Age64, Age65, Age66, Age67, Age68, Age69, Age70, Age71, Age72, Age73, Age74, Age75, Age76, Age77, Age78, Age79, Age80, Age81, Age82, Age83, Age84, Age85, Age86, Age87, Age88, Age89, Age90, Age91, Age92, Age93, Age94, Age95, Age96, Age97, Age98, Age99 FROM [SYS_Incidence_INVENTORY] where ID=" + incidencedata.dataidx.ToString());
            }
            else
            {
                loader.loadincidencefile(incidencedata.filename);
            }
            if (crdata.usetable)
            {
                executeQuery("INSERT INTO [SYS_CR] SELECT NULL, FunctionID, Endpoint, PoolingWeight, Seasonal_Metric, Study_Author, Study_Year, Start_Age, End_Age, Function, Beta, Adjusted, Parameter_1_Beta, A, Name_A, B, Name_B, C, Name_C, Cases, IncidenceEndpoint FROM [SYS_CR_INVENTORY] where ID=" + crdata.dataidx.ToString());
            }
            else
            {
                loader.loadcrfile(crdata.filename);
            }
            if (valdata.usetable)
            {
                executeQuery("INSERT INTO [SYS_Valuation] SELECT NULL, CRFunctionID, Endpoint, PoolingWeight, Seasonal_Metric, Study_Author, Study_Year, Start_Age, End_Age, Function, Beta, Adjusted, Parameter_1_Beta, A, Name_A, B, Name_B, C, Name_C, Cases, HealthEffect, ValuationMethod, Value, ApplyDiscount, IncidenceEndpoint FROM [SYS_Valuation_INVENTORY] where ID=" + valdata.dataidx.ToString());
            }
            else
            {
                loader.loadvaluationfile(valdata.filename);
            }

            dbConn.Close();
        }

        public void ComputeImpacts(bool generatetable, double valat, string popfile, string incidencefile, string crfile, string valfile, string outputfilename = "")
        {
            FileOrTable popdata = new FileOrTable
            {
                usetable = popfile == "",
                filename = popfile,
                dataidx = 2
            };
            FileOrTable incidencedata = new FileOrTable
            {
                usetable = incidencefile == "",
                filename = incidencefile,
                dataidx = 2
            };
            FileOrTable crdata = new FileOrTable
            {
                usetable = crfile == "",
                filename = crfile,
                dataidx = 1
            };
            FileOrTable valdata = new FileOrTable
            {
                usetable = valfile == "",
                filename = valfile,
                dataidx = 2
            };
            ComputeImpacts(generatetable, valat, popdata, incidencedata, crdata, valdata, outputfilename);
        }

        public void ComputeImpactsForYear(int year, bool generatetable, double valat, string popfile, string incidencefile, string crfile, string valfile, string outputfilename = "")
        {
            int modelyearidx = 1; //default 2016
            switch (year)
            {
                case 2016:
                    modelyearidx = 1;
                    break;
                case 2023:
                    modelyearidx = 2;
                    break;
                default:
                    modelyearidx = 3;
                    break;
            }


            FileOrTable popdata = new FileOrTable
            {
                usetable = popfile.Trim() == "",
                filename = popfile,
                dataidx = modelyearidx
            };
            FileOrTable incidencedata = new FileOrTable
            {
                usetable = incidencefile.Trim() == "",
                filename = incidencefile,
                dataidx = modelyearidx
            };
            FileOrTable crdata = new FileOrTable
            {
                usetable = crfile == "",
                filename = crfile,
                dataidx = 1 //only one option on file
            };
            FileOrTable valdata = new FileOrTable
            {
                usetable = valfile == "",
                filename = valfile,
                dataidx = modelyearidx
            };
            ComputeImpacts(generatetable, valat, popdata, incidencedata, crdata, valdata, outputfilename);
        }


        private double perannumvalue(int year, double weight, double factor)
        {
            return weight / Math.Pow(1 + factor, year);
        }

        private double adjustmentfactorfromdiscountrate(double factor)
        {
            double result = 0;
            result = result + perannumvalue(0, 0.3, factor);
            result = result + perannumvalue(1, 0.1, factor);
            result = result + perannumvalue(2, 0.1, factor);
            result = result + perannumvalue(3, 0.1, factor);
            result = result + perannumvalue(4, 0.1, factor);
            result = result + perannumvalue(5, 0.1, factor);
            result = result + perannumvalue(6, 0.0142857142857143, factor);
            result = result + perannumvalue(7, 0.0142857142857143, factor);
            result = result + perannumvalue(8, 0.0142857142857143, factor);
            result = result + perannumvalue(9, 0.0142857142857143, factor);
            result = result + perannumvalue(10, 0.0142857142857143, factor);
            result = result + perannumvalue(11, 0.0142857142857143, factor);
            result = result + perannumvalue(12, 0.0142857142857143, factor);
            result = result + perannumvalue(13, 0.0142857142857143, factor);
            result = result + perannumvalue(14, 0.0142857142857143, factor);
            result = result + perannumvalue(15, 0.0142857142857143, factor);
            result = result + perannumvalue(16, 0.0142857142857143, factor);
            result = result + perannumvalue(17, 0.0142857142857143, factor);
            result = result + perannumvalue(18, 0.0142857142857143, factor);
            result = result + perannumvalue(19, 0.0142857142857143, factor);
            return result;
        }


        public void ComputeImpacts(bool generatetable, double valat, FileOrTable popdata, FileOrTable incidencedata, FileOrTable crdata, FileOrTable valdata, string outputfilename = "")
        {
            //this is the default method that will restore POP, Incidence, CR and valuation from scneario with ID 1
            defaultauxtables(popdata, incidencedata, crdata, valdata);


            //now do the Enity stuff
            using (cobraEntities context = new cobraEntities(_EF_connectionstring))
            {
                Dictionary<string, Result> results_cr = new Dictionary<string, Result>();
                Dictionary<string, Result> results_valuation = new Dictionary<string, Result>();

                List<SYS_Destination> destinations = context.SYS_Destination.ToList();
                List<SYS_Valuation> valuationfunctions = context.SYS_Valuation.ToList();
                List<SYS_POP> populations = context.SYS_POP.ToList();
                List<SYS_Incidence> incidence = context.SYS_Incidence.ToList();
                List<SYS_CR> crfunctions = context.SYS_CR.ToList();

                List<string> Endpoints_cr = crfunctions.Select(c => c.Endpoint).Distinct().ToList();
                List<string> Endpoints_val = valuationfunctions.Select(c => c.Endpoint).Distinct().ToList();

                Dictionary<long?, SYS_POP> dict_pop = new Dictionary<long?, SYS_POP>(populations.Count());
                foreach (var item in populations)
                {
                    dict_pop.Add(item.DestinationID, item);
                }
                Dictionary<string, SYS_Incidence> dict_incidence = new Dictionary<string, SYS_Incidence>();
                foreach (var item in incidence)
                {
                    dict_incidence.Add(item.DestinationID.ToString() + "|" + item.Endpoint, item);
                }


                SYS_Incidence value;
                Result result_cr;
                Result result_valuation;
                SYS_Incidence incidencerow;

                foreach (var crfunc in crfunctions)
                {
                    string function = crfunc.Function;

                    function = function.Replace("EXP", "Exp");
                    function = function.Replace("exp", "Exp");

                    string cleanfunction = function.ToUpper().Replace(" ", "");

                    double metric_adjustment = 1;
                    if (crfunc.Seasonal_Metric.ToUpper() == "DAILY")
                    {
                        metric_adjustment = 365;
                    }
                    if (crfunc.Seasonal_Metric.ToUpper() == "OZONE")
                    {
                        metric_adjustment = 152;
                    }
                    /*if (crfunc.Seasonal_Metric.ToUpper() == "QUARTERLYMEAN")
                    {
                        metric_adjustment = 4;
                    }*/

                    foreach (var destination in destinations)
                    {
                        //fixed params
                        Expression e = new Expression(function);
                        double A = crfunc.A.GetValueOrDefault(0);
                        double B = crfunc.B.GetValueOrDefault(0);
                        double C = crfunc.C.GetValueOrDefault(0);
                        double Beta = crfunc.Beta.GetValueOrDefault(0);
                        double DELTAQ = destination.DELTA_FINAL_PM.GetValueOrDefault(0);
                        double DELTAO = destination.DELTA_FINAL_O3.GetValueOrDefault(0);
                        double Incidence = 0;

                        //year dependent but with the twist that pop and incidence are containing all year data
                        SYS_POP poprow = dict_pop[destination.destindx];
                        if (dict_incidence.TryGetValue(destination.destindx + "|" + crfunc.IncidenceEndpoint, out value))
                        {
                            incidencerow = value;
                        }
                        else
                        {
                            incidencerow = null;
                            Incidence = 0;
                        }

                        double poolingweight = crfunc.PoolingWeight.GetValueOrDefault(0);
                        for (long age = crfunc.Start_Age.GetValueOrDefault(0); age <= crfunc.End_Age.GetValueOrDefault(0); age++)
                        {
                            double POP = poprow.popat(age);
                            if (incidencerow != null)
                            {
                                Incidence = value.incidenceat(age);
                            }

                            //double result = (double)e.Evaluate() * poolingweight * metric_adjustment;
                            double result = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, DELTAO, POP, A, B, C) * poolingweight * metric_adjustment;

                            // check if there is an entry already to make pooling work
                            if (results_cr.TryGetValue(destination.destindx + "|" + crfunc.Endpoint, out result_cr))
                            {
                                result_cr.Value = result_cr.Value + result;
                            }
                            else
                            {
                                results_cr.Add(destination.destindx + "|" + crfunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = crfunc.Endpoint, Value = result });
                            }
                            // add to national total
                            if (results_cr.TryGetValue("nation|" + crfunc.Endpoint, out result_cr))
                            {
                                result_cr.Value = result_cr.Value + result;
                            }
                            else
                            {
                                results_cr.Add("nation|" + crfunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = crfunc.Endpoint, Value = result });
                            }




                        }
                    }
                }

                foreach (var valuefunc in valuationfunctions)
                {
                    string function = valuefunc.Function;

                    function = function.Replace("EXP", "Exp");
                    function = function.Replace("exp", "Exp");

                    string cleanfunction = function.ToUpper().Replace(" ", "");

                    double metric_adjustment = 1;
                    if (valuefunc.Seasonal_Metric.ToUpper() == "DAILY")
                    {
                        metric_adjustment = 365;
                    }
                    if (valuefunc.Seasonal_Metric.ToUpper() == "OZONE")
                    {
                        metric_adjustment = 152;
                    }

                    /*if (valuefunc.Seasonal_Metric.ToUpper() == "QUARTERLYMEAN")
                    {
                        metric_adjustment = 4;
                    }*/

                    foreach (var destination in destinations)
                    {
                        //fixed params
                        Expression e = new Expression(function);
                        double A = valuefunc.A.GetValueOrDefault(0);
                        double B = valuefunc.B.GetValueOrDefault(0);
                        double C = valuefunc.C.GetValueOrDefault(0);
                        double Beta = valuefunc.Beta.GetValueOrDefault(0);
                        double DELTAQ = destination.DELTA_FINAL_PM.GetValueOrDefault(0);
                        double DELTAO = destination.DELTA_FINAL_O3.GetValueOrDefault(0);
                        double Incidence = 0;

                        //year dependent but with the twist that pop and incidence are containing all year data
                        SYS_POP poprow = dict_pop[destination.destindx];
                        if (dict_incidence.TryGetValue(destination.destindx + "|" + valuefunc.IncidenceEndpoint, out value))
                        {
                            incidencerow = value;
                        }
                        else
                        {
                            incidencerow = null;
                            Incidence = 0;
                        }

                        double poolingweight = valuefunc.PoolingWeight.GetValueOrDefault(0);

                        for (long age = valuefunc.Start_Age.GetValueOrDefault(0); age <= valuefunc.End_Age.GetValueOrDefault(0); age++)
                        {
                            double POP = poprow.popat(age);
                            if (incidencerow != null)
                            {
                                Incidence = value.incidenceat(age);
                            }

                            double numCases = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, DELTAO, POP, A, B, C) * poolingweight * metric_adjustment;
                            double valueCases = 0;

                            if (valat == 0 || valuefunc.ApplyDiscount == "NO")
                            {
                                //no discount rate applied just take the undiscounted value
                                valueCases = numCases * valuefunc.Value.GetValueOrDefault(0) * 1.1225;
                            }
                            else
                            { //apply custom discount rate
                              //we are assuming that valuefunc.Value is the UNDISCOUNTED rate so now we are going to apply the discount rate
                                double valtarget = adjustmentfactorfromdiscountrate(valat / 100);

                                valueCases = numCases * valuefunc.Value.GetValueOrDefault(0) * valtarget * 1.1225;

                                //result = result * valuefunc.ApplyDiscount.GetValueOrDefault(0) * 1.1225;
                            }


                            // check if there is an entry already to make pooling work
                            if (results_valuation.TryGetValue(destination.destindx + "|" + valuefunc.Endpoint, out result_valuation))
                            {
                                result_valuation.Value = result_valuation.Value + valueCases;
                            }
                            else
                            {
                                results_valuation.Add(destination.destindx + "|" + valuefunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = valueCases });
                            }
                            // add to national totals as well
                            if (results_valuation.TryGetValue("nation|" + valuefunc.Endpoint, out result_valuation))
                            {
                                result_valuation.Value = result_valuation.Value + valueCases;
                            }
                            else
                            {
                                results_valuation.Add("nation|" + valuefunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = valueCases });
                            }


                        }
                    }
                }

                //ok, now we have it all, output to filename// need 
                if (!generatetable)
                {
                    Console.WriteLine("Writing to CSV:");

                    // these are the header row names
                    string[] colnames = new string[]
                    {
    "ID",
    "destindx",
    "FIPS",
    "State",
    "County",
    "Base PM 2.5",
    "Control PM 2.5",
    "Delta PM 2.5",
    "Base O3",
    "Control O3",
    "Delta O3",
    "$ Total Health Benefits(low estimate)",
    "$ Total Health Benefits(high estimate)",
    "Total Mortality(low estimate)",
    "$ Total Mortality(low estimate)",
    "Total Mortality(high estimate)",
    "$ Total Mortality(high estimate)",
    "PM Mortality, All Cause (low)",
    "$ PM Mortality, All Cause (low)",
    "PM Mortality, All Cause (high)",
    "$ PM Mortality, All Cause (high)",
    "PM Infant Mortality",
    "$ PM Infant Mortality",
    "Total O3 Mortality",
    "$ Total O3 Mortality",
    "O3 Mortality (Short-term exposure)",
    "$ O3 Mortality (Short term exposure)",
    "O3 Mortality (Long-term exposure)",
    "$ O3 Mortality (Long-term exposure)",
    "Total Asthma Symptoms",
    "$ Total Asthma Symptoms",
    "PM Asthma Symptoms, Albuterol use",
    "$ PM Asthma Symptoms, Albuterol use",
    "O3 Asthma Symptoms, Chest Tightness",
    "$ O3 Asthma Symptoms, Chest Tightness",
    "O3 Asthma Symptoms, Cough",
    "$ O3 Asthma Symptoms, Cough",
    "O3 Asthma Symptoms, Shortness of Breath",
    "$ O3 Asthma Symptoms, Shortness of Breath",
    "O3 Asthma Symptoms, Wheeze",
    "$ O3 Asthma Symptoms, Wheeze",
    "Total Incidence, Asthma",
    "$ Total Incidence, Asthma",
    "PM Incidence, Asthma",
    "$ PM Incidence, Asthma",
    "O3 Incidence, Asthma",
    "$ O3 Incidence, Asthma",
    "Total Incidence, Hay Fever/Rhinitis",
    "$ Total Incidence, Hay Fever/Rhinitis",
    "PM Incidence, Hay Fever/Rhinitis",
    "$ PM Incidence, Hay Fever/Rhinitis",
    "O3 Incidence, Hay Fever/Rhinitis",
    "$ O3 Incidence, Hay Fever/Rhinitis",
    "Total ER Visits, Respiratory",
    "$ Total ER Visits, Respiratory",
    "PM ER Visits, Respiratory",
    "$ PM ER Visits, Respiratory",
    "O3 ER Visits, Respiratory",
    "$ O3 ER Visits, Respiratory",
    "Total Hospital Admits, All Respiratory",
    "$ Total Hospital Admits All Respiratory",
    "PM Hospital Admits, All Respiratory",
    "$ PM Hospital Admits All Respiratory",
    "O3 Hospital Admits, All Respiratory",
    "$ O3 Hospital Admits All Respiratory",
    "PM Nonfatal Heart Attacks",
    "$ PM Nonfatal Heart Attacks",
    "PM Minor Restricted Activity Days",
    "$ PM Minor Restricted Activity Days",
    "PM Work Loss Days",
    "$ PM Work Loss Days",
    "PM Incidence Lung Cancer",
    "$ PM Incidence Lung Cancer",
    "PM HA Cardio Cerebro and Peripheral Vascular Disease",
    "$ PM HA Cardio Cerebro and Peripheral Vascular Disease",
    "PM HA Alzheimers Disease",
    "$ PM HA Alzheimers Disease",
    "PM HA Parkinsons Disease",
    "$ PM HA Parkinsons Disease",
    "PM Incidence Stroke",
    "$ PM Incidence Stroke",
    "PM Incidence Out of Hospital Cardiac Arrest",
    "$ PM Incidence Out of Hospital Cardiac Arrest",
    "PM ER visits All Cardiac Outcomes",
    "$ PM ER visits All Cardiac Outcomes",
    "O3 ER Visits, Asthma",
    "$ O3 ER Visits, Asthma",
    "O3 School Loss Days, All Cause",
    "$ O3 School Loss Days, All Cause"
};


                    // Placeholder for totals, initialize with 0
                    double[] totals = new double[colnames.Length - 5];
                    // List to temporarily store all rows
                    List<List<object>> allRows = new List<List<object>>();


                    foreach (var destination in destinations)
                    {
                        int i = 0;
                        List<object> row = new List<object>
                            {
                                destination.ID,
                                destination.destindx
                            };

                        // Get FIPS, State, and County from the dictionary
                        List<SYS_Dict> dict = context.SYS_Dict.ToList();
                        var loc = dict.First(d => d.SOURCEINDX == destination.destindx);
                        row.Add(loc.FIPS);
                        row.Add(loc.STNAME);
                        row.Add(loc.CYNAME);

                        row.Add(destination.BASE_FINAL_PM);
                        totals[i++] += destination.BASE_FINAL_PM ?? 0.0;

                        row.Add(destination.CTRL_FINAL_PM);
                        totals[i++] += destination.CTRL_FINAL_PM ?? 0.0;
                        row.Add(destination.DELTA_FINAL_PM);
                        totals[i++] += destination.DELTA_FINAL_PM ?? 0.0;
                        row.Add(destination.BASE_FINAL_O3);
                        totals[i++] += destination.BASE_FINAL_O3 ?? 0.0;

                        row.Add(destination.CTRL_FINAL_O3);
                        totals[i++] += destination.CTRL_FINAL_O3 ?? 0.0;
                        row.Add(destination.DELTA_FINAL_O3);
                        totals[i++] += destination.DELTA_FINAL_O3 ?? 0.0;

                        // get total health benefits low and high vals
                        double valSum = 0;
                        foreach (var vale in Endpoints_val)
                        {
                            valSum += results_valuation[destination.destindx + "|" + vale].Value;
                        }
                        double totalLow = valSum - results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value;
                        double totalHigh = valSum - results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value;
                        row.Add(totalLow);
                        totals[i++] += totalLow;
                        row.Add(totalHigh);
                        totals[i++] += totalHigh;

                        double val = 0;
                        /* total mortality (low) */
                        val = results_cr[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value +
                           results_cr[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value +
                            results_cr[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value +
                          results_cr[destination.destindx + "|" + "PM Infant Mortality"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        val = results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value +
                            results_valuation[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value +
                             results_valuation[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value +
                            results_valuation[destination.destindx + "|" + "PM Infant Mortality"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        /* total mortality (high) */
                        val = results_cr[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value +
                          results_cr[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value +
                           results_cr[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value +
                         results_cr[destination.destindx + "|" + "PM Infant Mortality"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value +
results_valuation[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value +
 results_valuation[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value +
results_valuation[destination.destindx + "|" + "PM Infant Mortality"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        /* total PM mortality low */
                        val = results_cr[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        /* total PM mortality high */
                        val = results_cr[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        /* infant mortality */
                        val = results_cr[destination.destindx + "|" + "PM Infant Mortality"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Infant Mortality"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        /* Total O3 Mortality */
                        val = results_cr[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value + results_cr[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value + results_valuation[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value;
                        row.Add(val);
                        totals[i++] += val;


                        /* Total Asthma Symptoms*/
                        val = results_cr[destination.destindx + "|" + "PM Asthma Symptoms, Albuterol use"].Value +
                            results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Chest Tightness"].Value +
                            results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Cough"].Value +
                            results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Shortness of Breath"].Value +
                            results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Wheeze"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Asthma Symptoms, Albuterol use"].Value +
                           results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Chest Tightness"].Value +
                           results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Cough"].Value +
                          results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Shortness of Breath"].Value +
                          results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Wheeze"].Value;
                        row.Add(val);
                        totals[i++] += val;


                        val = results_cr[destination.destindx + "|" + "PM Asthma Symptoms, Albuterol use"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Asthma Symptoms, Albuterol use"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Chest Tightness"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Chest Tightness"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Cough"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Cough"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Shortness of Breath"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Shortness of Breath"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Wheeze"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Wheeze"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        /* Total Asthma Incidence */
                        val = results_cr[destination.destindx + "|" + "PM Incidence, Asthma"].Value +
                        results_cr[destination.destindx + "|" + "O3 Incidence, Asthma"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Incidence, Asthma"].Value +
                    results_valuation[destination.destindx + "|" + "O3 Incidence, Asthma"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        val = results_cr[destination.destindx + "|" + "PM Incidence, Asthma"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Incidence, Asthma"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 Incidence, Asthma"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 Incidence, Asthma"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        /* total hayfever/rhinitis */
                        val = results_cr[destination.destindx + "|" + "PM Incidence, Hay Fever/Rhinitis"].Value +
                        results_cr[destination.destindx + "|" + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Incidence, Hay Fever/Rhinitis"].Value +
                        results_valuation[destination.destindx + "|" + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        val = results_cr[destination.destindx + "|" + "PM Incidence, Hay Fever/Rhinitis"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Incidence, Hay Fever/Rhinitis"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        /* total er respiratory*/
                        val = results_cr[destination.destindx + "|" + "PM ER visits, respiratory"].Value +
                        results_cr[destination.destindx + "|" + "O3 ER visits, respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM ER visits, respiratory"].Value +
                        results_valuation[destination.destindx + "|" + "O3 ER visits, respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM ER visits, respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM ER visits, respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 ER visits, respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 ER visits, respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        /* Total hopsital admits, all respiratory */
                        val = results_cr[destination.destindx + "|" + "PM HA, All Respiratory"].Value +
                       results_cr[destination.destindx + "|" + "O3 HA, All Respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM HA, All Respiratory"].Value +
                        results_valuation[destination.destindx + "|" + "O3 HA, All Respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM HA, All Respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM HA, All Respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 HA, All Respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 HA, All Respiratory"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        val = results_cr[destination.destindx + "|" + "PM Acute Myocardial Infarction, Nonfatal"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Acute Myocardial Infarction, Nonfatal"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM Minor Restricted Activity Days"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Minor Restricted Activity Days"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM Work Loss Days"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Work Loss Days"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM Incidence, Lung Cancer"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Incidence, Lung Cancer"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM HA, Cardio-, Cerebro- and Peripheral Vascular Disease"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM HA, Cardio-, Cerebro- and Peripheral Vascular Disease"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM HA, Alzheimers Disease"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM HA, Alzheimers Disease"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM HA, Parkinsons Disease"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM HA, Parkinsons Disease"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM Incidence, Stroke"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Incidence, Stroke"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM Incidence, Out of Hospital Cardiac Arrest"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM Incidence, Out of Hospital Cardiac Arrest"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "PM ER visits, All Cardiac Outcomes"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "PM ER visits, All Cardiac Outcomes"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 Emergency Room Visits, Asthma"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 Emergency Room Visits, Asthma"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_cr[destination.destindx + "|" + "O3 School Loss Days, All Cause"].Value;
                        row.Add(val);
                        totals[i++] += val;
                        val = results_valuation[destination.destindx + "|" + "O3 School Loss Days, All Cause"].Value;
                        row.Add(val);
                        totals[i++] += val;

                        allRows.Add(row);
                    } //end destinations loop

                    using (TextWriter fileWriter = File.CreateText(outputfilename))
                    {
                        CsvWriter csv = new CsvWriter(fileWriter, System.Globalization.CultureInfo.CurrentCulture);


                        //write the header row
                        foreach (var colname in colnames)
                        {
                            csv.WriteField(colname);
                        }
                        csv.NextRecord();
                        // Now get the corresponding data
                        // Write the totals row before the data rows
                        List<object> totalRow = new List<object> { "", "", "", "", "" };
                        totalRow.AddRange(totals.Cast<object>());
                        for (int j = 0; j < totalRow.Count; j++)
                        {
                            if (j < 5)
                            {
                                csv.WriteField(totalRow[j]);
                            }
                            else
                            {
                                csv.WriteField("Total: " + totalRow[j]);
                            }
                        }
                        csv.NextRecord();

                        // Write all the collected destination rows
                        foreach (var row in allRows)
                        {
                            foreach (var field in row)
                            {
                                csv.WriteField(field);
                            }
                            csv.NextRecord();
                        }
                        //add additional total row on bottom
                        for (int j = 0; j < totalRow.Count; j++)
                        {
                            if (j < 5)
                            {
                                csv.WriteField(totalRow[j]);
                            }
                            else
                            {
                                csv.WriteField("Total: " + totalRow[j]);
                            }
                        }
                        csv.NextRecord();

                    }
                }
                else
                {
                    /*executeQuery("DROP TABLE IF EXISTS SYS_Results");
                    string pre = "CREATE TABLE \"SYS_Results\"("
                        + "`ID`	INTEGER PRIMARY KEY AUTOINCREMENT,"
                        + "`destindx`	INTEGER,"
                        + "`FIPS`	TEXT,"
                        + "`STATE`	TEXT,"
                        + "`COUNTY`	TEXT,"
                        + "`BASE_FINAL_PM`	REAL,"
                        + "`CTRL_FINAL_PM`	REAL,"
                        + "`DELTA_FINAL_PM`	REAL, ";

                    string health = String.Join(",", Endpoints_cr.Select(x => "`"+x+"`"+" Real"));
                    string valuation = String.Join(",", Endpoints_val.Select(x => "`$ " + x + "`" + " Real"));

                    executeQuery(pre + health + ", " + valuation + ")");
                    */
                    clearTable("SYS_Results");

                    List<SYS_Dict> dict = context.SYS_Dict.ToList();

                    List<SYS_Results> results = new List<SYS_Results>();
                    foreach (var destination in destinations)
                    {
                        SYS_Results result_record = new SYS_Results();
                        result_record.destindx = destination.destindx;
                        result_record.BASE_FINAL_PM = destination.BASE_FINAL_PM;
                        result_record.CTRL_FINAL_PM = destination.CTRL_FINAL_PM;
                        result_record.DELTA_FINAL_PM = destination.DELTA_FINAL_PM;
                        result_record.BASE_FINAL_O3 = destination.BASE_FINAL_O3;
                        result_record.CTRL_FINAL_O3 = destination.CTRL_FINAL_O3;
                        result_record.DELTA_FINAL_O3 = destination.DELTA_FINAL_O3;
                        var loc = dict.Where(d => d.SOURCEINDX == result_record.destindx).First();
                        result_record.FIPS = loc.FIPS;
                        result_record.STATE = loc.STNAME;
                        result_record.COUNTY = loc.CYNAME;



                        result_record.PM_Acute_Myocardial_Infarction_Nonfatal = results_cr[destination.destindx + "|" + "PM Acute Myocardial Infarction, Nonfatal"].Value;


                        result_record.PM_HA_All_Respiratory = results_cr[destination.destindx + "|" + "PM HA, All Respiratory"].Value;
                        result_record.PM_Minor_Restricted_Activity_Days = results_cr[destination.destindx + "|" + "PM Minor Restricted Activity Days"].Value;
                        result_record.PM_Mortality_All_Cause__low_ = results_cr[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value;
                        result_record.PM_Mortality_All_Cause__high_ = results_cr[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value;
                        result_record.PM_Infant_Mortality = results_cr[destination.destindx + "|" + "PM Infant Mortality"].Value;
                        result_record.PM_Work_Loss_Days = results_cr[destination.destindx + "|" + "PM Work Loss Days"].Value;
                        result_record.PM_Incidence_Lung_Cancer = results_cr[destination.destindx + "|" + "PM Incidence, Lung Cancer"].Value;

                        result_record.PM_Incidence_Hay_Fever_Rhinitis = results_cr[destination.destindx + "|" + "PM Incidence, Hay Fever/Rhinitis"].Value;
                        result_record.PM_Incidence_Asthma = results_cr[destination.destindx + "|" + "PM Incidence, Asthma"].Value;
                        result_record.PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease = results_cr[destination.destindx + "|" + "PM HA, Cardio-, Cerebro- and Peripheral Vascular Disease"].Value;
                        result_record.PM_HA_Alzheimers_Disease = results_cr[destination.destindx + "|" + "PM HA, Alzheimers Disease"].Value;
                        result_record.PM_HA_Parkinsons_Disease = results_cr[destination.destindx + "|" + "PM HA, Parkinsons Disease"].Value;
                        result_record.PM_Incidence_Stroke = results_cr[destination.destindx + "|" + "PM Incidence, Stroke"].Value;
                        result_record.PM_Incidence_Out_of_Hospital_Cardiac_Arrest = results_cr[destination.destindx + "|" + "PM Incidence, Out of Hospital Cardiac Arrest"].Value;
                        result_record.PM_Asthma_Symptoms_Albuterol_use = results_cr[destination.destindx + "|" + "PM Asthma Symptoms, Albuterol use"].Value;
                        result_record.PM_HA_Respiratory2 = results_cr[destination.destindx + "|" + "PM HA, Respiratory-2"].Value;
                        result_record.PM_ER_visits_respiratory = results_cr[destination.destindx + "|" + "PM ER visits, respiratory"].Value;
                        result_record.PM_ER_visits_All_Cardiac_Outcomes = results_cr[destination.destindx + "|" + "PM ER visits, All Cardiac Outcomes"].Value;

                        result_record.O3_ER_visits_respiratory = results_cr[destination.destindx + "|" + "O3 ER visits, respiratory"].Value;
                        result_record.O3_HA_All_Respiratory = results_cr[destination.destindx + "|" + "O3 HA, All Respiratory"].Value;
                        result_record.O3_Incidence_Hay_Fever_Rhinitis = results_cr[destination.destindx + "|" + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                        result_record.O3_Incidence_Asthma = results_cr[destination.destindx + "|" + "O3 Incidence, Asthma"].Value;
                        result_record.O3_Asthma_Symptoms_Chest_Tightness = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Chest Tightness"].Value;
                        result_record.O3_Asthma_Symptoms_Cough = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Cough"].Value;
                        result_record.O3_Asthma_Symptoms_Shortness_of_Breath = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Shortness of Breath"].Value;
                        result_record.O3_Asthma_Symptoms_Wheeze = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Wheeze"].Value;
                        result_record.O3_ER_Visits_Asthma = results_cr[destination.destindx + "|" + "O3 Emergency Room Visits, Asthma"].Value;
                        result_record.O3_School_Loss_Days = results_cr[destination.destindx + "|" + "O3 School Loss Days, All Cause"].Value;
                        result_record.O3_Mortality_Longterm_exposure = results_cr[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value;
                        result_record.O3_Mortality_Shortterm_exposure = results_cr[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value;


                        result_record.C__PM_Acute_Myocardial_Infarction_Nonfatal = results_valuation[destination.destindx + "|" + "PM Acute Myocardial Infarction, Nonfatal"].Value;


                        result_record.C__PM_Resp_Hosp_Adm = results_valuation[destination.destindx + "|" + "PM HA, All Respiratory"].Value;
                        result_record.C__PM_Minor_Restricted_Activity_Days = results_valuation[destination.destindx + "|" + "PM Minor Restricted Activity Days"].Value;
                        result_record.C__PM_Mortality_All_Cause__low_ = results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value;
                        result_record.C__PM_Mortality_All_Cause__high_ = results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value;
                        result_record.C__PM_Infant_Mortality = results_valuation[destination.destindx + "|" + "PM Infant Mortality"].Value;

                        result_record.C__PM_Work_Loss_Days = results_valuation[destination.destindx + "|" + "PM Work Loss Days"].Value;
                        result_record.C__PM_Incidence_Lung_Cancer = results_valuation[destination.destindx + "|" + "PM Incidence, Lung Cancer"].Value;

                        result_record.C__PM_Incidence_Hay_Fever_Rhinitis = results_valuation[destination.destindx + "|" + "PM Incidence, Hay Fever/Rhinitis"].Value;
                        result_record.C__PM_Incidence_Asthma = results_valuation[destination.destindx + "|" + "PM Incidence, Asthma"].Value;
                        result_record.C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease = results_valuation[destination.destindx + "|" + "PM HA, Cardio-, Cerebro- and Peripheral Vascular Disease"].Value;
                        result_record.C__PM_HA_Alzheimers_Disease = results_valuation[destination.destindx + "|" + "PM HA, Alzheimers Disease"].Value;
                        result_record.C__PM_HA_Parkinsons_Disease = results_valuation[destination.destindx + "|" + "PM HA, Parkinsons Disease"].Value;
                        result_record.C__PM_Incidence_Stroke = results_valuation[destination.destindx + "|" + "PM Incidence, Stroke"].Value;
                        result_record.C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest = results_valuation[destination.destindx + "|" + "PM Incidence, Out of Hospital Cardiac Arrest"].Value;
                        result_record.C__PM_Asthma_Symptoms_Albuterol_use = results_valuation[destination.destindx + "|" + "PM Asthma Symptoms, Albuterol use"].Value;
                        result_record.C__PM_HA_Respiratory2 = results_valuation[destination.destindx + "|" + "PM HA, Respiratory-2"].Value;
                        result_record.C__PM_ER_visits_respiratory = results_valuation[destination.destindx + "|" + "PM ER visits, respiratory"].Value;
                        result_record.C__PM_ER_visits_All_Cardiac_Outcomes = results_valuation[destination.destindx + "|" + "PM ER visits, All Cardiac Outcomes"].Value;

                        result_record.C__O3_ER_visits_respiratory = results_valuation[destination.destindx + "|" + "O3 ER visits, respiratory"].Value;
                        result_record.C__O3_HA_All_Respiratory = results_valuation[destination.destindx + "|" + "O3 HA, All Respiratory"].Value;
                        result_record.C__O3_Incidence_Hay_Fever_Rhinitis = results_valuation[destination.destindx + "|" + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                        result_record.C__O3_Incidence_Asthma = results_valuation[destination.destindx + "|" + "O3 Incidence, Asthma"].Value;
                        result_record.C__O3_Asthma_Symptoms_Chest_Tightness = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Chest Tightness"].Value;
                        result_record.C__O3_Asthma_Symptoms_Cough = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Cough"].Value;
                        result_record.C__O3_Asthma_Symptoms_Shortness_of_Breath = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Shortness of Breath"].Value;
                        result_record.C__O3_Asthma_Symptoms_Wheeze = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Wheeze"].Value;
                        result_record.C__O3_ER_Visits_Asthma = results_valuation[destination.destindx + "|" + "O3 Emergency Room Visits, Asthma"].Value;
                        result_record.C__O3_School_Loss_Days = results_valuation[destination.destindx + "|" + "O3 School Loss Days, All Cause"].Value;
                        result_record.C__O3_Mortality_Longterm_exposure = results_valuation[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value;
                        result_record.C__O3_Mortality_Shortterm_exposure = results_valuation[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value;

                        results.Add(result_record);
                    }
                    context.SYS_Results.AddRange(results);
                    context.SaveChanges();


                }

            }

        }



    }
}
