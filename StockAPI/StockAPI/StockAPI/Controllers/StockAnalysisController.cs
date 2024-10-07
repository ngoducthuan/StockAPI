using Microsoft.AspNetCore.Mvc;
using StockAPI.Models;
using System.Text;
using Newtonsoft.Json;
using StockAPI.Models.Requests;
using System.Net;

using StockAPI.DTOs;

using System;
using StockAPI.Models.Response;
using static System.Net.WebRequestMethods;


namespace StockAPI.Controllers;
[Route("[controller]/api")]
[ApiController]
public class StockAnalysisController : ControllerBase
{
    private readonly IHttpClientFactory _clientFactory;

    public StockAnalysisController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAsync([FromQuery] string[] ids)
    {
        //void WriteToExcel(MemoryStream stream, List<Stock> stocks)
        //{
        //    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
        //    {
        //        writer.WriteLine("Id,Symbol,MC,C,F,LastPrice,LastVolume,Lot,ot,ChangePc,AvePrice,HighPrice,LowPrice,fBVol,fBValue,fSVolume,fSValue,fRoom,g1,g2,g3,g4,g5,g6,g7,mp,CWUnderlying,CWIssuerName,CWType,CWMaturityDate,CWLastTradingDate,CWExcersisePrice,CWExerciseRatio,CWListedShare,sType,sBenefit");

        //        foreach (var stock in stocks)
        //        {
        //            Console.WriteLine(stock);
        //            writer.WriteLine($"{stock.Id},{stock.Sym},{stock.Mc}," +
        //                $"{stock.C},{stock.F},{stock.LastPrice},{stock.LastVolume}," +
        //                $"{stock.Lot},{stock.Ot},{stock.ChangePc},{stock.AvePrice},{stock.HighPrice}," +
        //                $"{stock.LowPrice},{stock.FBVol},{stock.FBValue},{stock.FSVolume},{stock.FSValue}," +
        //                $"{stock.FRoom},{stock.G1},{stock.G2},{stock.G3},{stock.G4},{stock.G5},{stock.G6}" +
        //                $",{stock.G7},{stock.Mp},{stock.CWUnderlying},{stock.CWIssuerName},{stock.CWType}" +
        //                $",{stock.CWMaturityDate},{stock.CWLastTradingDate},{stock.CWExcersisePrice},{stock.CWExerciseRatio},{stock.CWListedShare},{stock.SType},{stock.SBenefit}");
        //        }
        //    }
        //}
        //var client = _clientFactory.CreateClient();
        //var response = await client.GetAsync($"https://bgapidatafeed.vps.com.vn/getliststockdata/{string.Join(", ", ids)}");

        //if (response.IsSuccessStatusCode)
        //{
        //    var content = await response.Content.ReadAsStringAsync();
        //    var stocks = JsonConvert.DeserializeObject<Stock[]>(content);
        //    if (stocks == null || stocks.Length == 0)
        //    {
        //        return BadRequest("id not found");
        //    }
        //    MemoryStream stream = new MemoryStream();

        //    WriteToExcel(stream, stocks.ToList());

        //    stream.Seek(0, SeekOrigin.Begin);

        //    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "stock_data.csv");
        //}
        //else
        //{
        //    return StatusCode((int)response.StatusCode, "Failed to get data from the API.");
        //}
        var idsString = string.Join(",", ids);
        Console.WriteLine("Oke"+idsString);
        var client = _clientFactory.CreateClient();
        var response = await client.GetAsync($"https://histdatafeed.vps.com.vn/tradingview/history?symbol={idsString}&resolution=1D&from=1546300800&to=1810903972");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var stocks = JsonConvert.DeserializeObject<StocksOverTime>(content);

            if (stocks == null)
            {
                return BadRequest("id not found");
            }

            var stockDataArray = MapToSOToSOR(stocks);

            // Create a CSV content
            var csv = new StringBuilder();
            csv.AppendLine("Time,Open,High,Low,Close,Volume");

            foreach (var stockData in stockDataArray)
            {
                csv.AppendLine($"{stockData.Time},{stockData.Open},{stockData.High},{stockData.Low},{stockData.Close},{stockData.Volume}");
            }

            // Return CSV file as a downloadable file
            byte[] buffer = Encoding.UTF8.GetBytes(csv.ToString());
            return File(buffer, "text/csv", $"{idsString}_stock_data.csv");
        }
        else
        {
            return StatusCode((int)response.StatusCode, "Failed to get data from the API.");
        }
    }

    private DateTime UnixTimeStampToDateTime(long unixTimestamp)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        return dateTimeOffset.DateTime;
    }

    [HttpPost("compare")]
    public async Task<IActionResult> Compare(CompareRequest request)
    {
        var client = _clientFactory.CreateClient();
        var firstResponse = await client.GetAsync($"https://histdatafeed.vps.com.vn/tradingview/history?symbol={request.FirstCode}&resolution=1D&from=1546300800&to=1810903972");
        var secondResponse = await client.GetAsync($"https://histdatafeed.vps.com.vn/tradingview/history?symbol={request.SecondCode}&resolution=1D&from=1546300800&to=1810903972");
        if (firstResponse.IsSuccessStatusCode && secondResponse.IsSuccessStatusCode)
        {
            var first = JsonConvert.DeserializeObject<CompareResponse>(await firstResponse.Content.ReadAsStringAsync());
            var second = JsonConvert.DeserializeObject<CompareResponse>(await secondResponse.Content.ReadAsStringAsync());
            if (first is null || second is null)
            {
                return BadRequest("id not found");
            }

            var listFirst = new List<CompareObjectResponse>();
            var listSecond = new List<CompareObjectResponse>();

            for (int i = 0; i < first.Time.Count; i++)
            {
                listFirst.Add(
                    new CompareObjectResponse
                    {
                        Time = UnixTimeStampToDateTime(first.Time[i]),
                        Close = first.Close[i],
                        Hight = first.Hight[i],
                        Low = first.Low[i],
                        Open = first.Open[i],
                        Volume = first.Volume[i],
                    });

            }

            for (int i = 0; i < second.Time.Count; i++)
            {
                listSecond.Add(
                    new CompareObjectResponse
                    {
                        Time = UnixTimeStampToDateTime(second.Time[i]),
                        Close = second.Close[i],
                        Hight = second.Hight[i],
                        Low = second.Low[i],
                        Open = second.Open[i],
                        Volume = second.Volume[i],
                    });

            }

            var response = new
            {
                listFirst,
                listSecond,
            };
            return Ok(response);
        }
        else
        {
            return StatusCode((int)firstResponse.StatusCode, "Failed to get data from the API.");
        }
    }

    [HttpGet("/{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var client = _clientFactory.CreateClient();
        var response = await client.GetAsync($"https://histdatafeed.vps.com.vn/tradingview/history?symbol={id}&resolution=1D&from=1546300800&to=1810903972");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var stocks = JsonConvert.DeserializeObject<StocksOverTime>(content);
            if (stocks == null)
            {
                return BadRequest("id not found");
            }
            return Ok(MapToSOToSOR(stocks));
        }
        else
        {
            return StatusCode((int)response.StatusCode, "Failed to get data from the API.");
        }
    }

    [HttpGet("/getall/{exchanges}")]
    public async Task<IActionResult> GetByStockExchanges(string exchanges)
    {

        string listVN30 = "https://bgapidatafeed.vps.com.vn/getliststockdata/ACB,BCM,BID,BVH,CTG,FPT,GAS,GVR,HDB,HPG,MBB,MSN,MWG,PLX,POW,SAB,SHB,SSB," +
            "SSI,STB,TCB,TPB,VCB,VHM,VIB,VIC,VJC,VNM,VPB,VRE";
        //string listVN30 = "https://query1.finance.yahoo.com/v7/finance/quote?symbols=AAPL";
        string hose = "https://bgapidatafeed.vps.com.vn/getliststockdata/AAA,AAM,AAT,ABR,ABS,ABT,ACB,ACC,ACG,ACL,ADG,ADP,ADS,AGG,AGM,AGR,ANV,APC,APG," +
            "APH,ASG,ASM,ASP,AST,BAF,BBC,BCE,BCG,BCM,BFC,BHN,BIC,BID,BKG,BMC,BMI,BMP,BRC,BSI,BTP,BTT,BVH,BWE,C32,C47,CAV,CCI,CCL,CDC,CHP,CIG,CII,CKG," +
            "CLC,CLL,CLW,CMG,CMV,CMX,CNG,COM,CRC,CRE,CSM,CSV,CTD,CTF,CTG,CTI,CTR,CTS,CVT,D2D,DAG,DAH,DAT,DBC,DBD,DBT,DC4,DCL,DCM,DGC,DGW,DHA,DHC,DHG," +
            "DHM,DIG,DLG,DMC,DPG,DPM,DPR,DQC,DRC,DRH,DRL,DSN,DTA,DTL,DTT,DVP,DXG,DXS,DXV,EIB,ELC,EMC,EVE,EVF,EVG,FCM,FCN,FDC,FIR,FIT,FMC,FPT,FRT,FTS," +
            "GAS,GDT,GEG,GEX,GIL,GMC,GMD,GMH,GSP,GTA,GVR,HAG,HAH,HAP,HAR,HAS,HAX,HBC,HCD,HCM,HDB,HDC,HDG,HHP,HHS,HHV,HID,HII,HMC,HNA,HNG,HPG,HPX,HQC," +
            "HRC,HSG,HSL,HT1,HTG,HTI,HTL,HTN,HTV,HU1,HUB,HVH,HVN,HVX,ICT,IDI,IJC,ILB,IMP,ITA,ITC,ITD,JVC,KBC,KDC,KDH,KHG,KHP,KMR,KOS,KPF,KSB,L10,LAF," +
            "LBM,LCG,LDG,LEC,LGC,LGL,LHG,LIX,LM8,LPB,LSS,MBB,MCP,MDG,MHC,MIG,MSB,MSH,MSN,MWG,NAF,NAV,NBB,NCT,NHA,NHH,NHT,NKG,NLG,NNC,NO1,NSC,NT2,NTL," +
            "NVL,NVT,OCB,OGC,OPC,ORS,PAC,PAN,PC1,PDN,PDR,PET,PGC,PGD,PGI,PGV,PHC,PHR,PIT,PJT,PLP,PLX,PMG,PNC,PNJ,POM,POW,PPC,PSH,PTB,PTC,PTL,PVD,PVP," +
            "PVT,QBS,QCG,QNP,RAL,RDP,REE,ROS,S4A,SAB,SAM,SAV,SBA,SBG,SBT,SBV,SC5,SCD,SCR,SCS,SFC,SFG,SFI,SGN,SGR,SGT,SHA,SHB,SHI,SHP,SIP,SJD,SJF,SJS," +
            "SKG,SMA,SMB,SMC,SPM,SRC,SRF,SSB,SSC,SSI,ST8,STB,STG,STK,SVC,SVD,SVI,SVT,SZC,SZL,TBC,TCB,TCD,TCH,TCI,TCL,TCM,TCO,TCR,TCT,TDC,TDG,TDH,TDM," +
            "TDP,TDW,TEG,THG,THI,TIP,TIX,TLD,TLG,TLH,TMP,TMS,TMT,TN1,TNA,TNC,TNH,TNI,TNT,TPB,TPC,TRA,TRC,TSC,TTA,TTE,TTF,TV2,TVB,TVS,TVT,TYA,UIC,VAF," +
            "VCA,VCB,VCF,VCG,VCI,VDP,VDS,VFG,VGC,VHC,VHM,VIB,VIC,VID,VIP,VIX,VJC,VMD,VND,VNE,VNG,VNL,VNM,VNS,VOS,VPB,VPD,VPG,VPH,VPI,VPS,VRC,VRE,VSC," +
            "VSH,VSI,VTB,VTO,YBM,YEG";
        string hnx = "https://bgapidatafeed.vps.com.vn/getliststockdata/AAV,ADC,ALT,AMC,AME,AMV,API,APS,ARM,ATS,BAB,BAB122030,BAB122031,BAB122032," +
            "BAB123005,BAB123006,BAB123007,BAB123030,BAB123031,BAB123032,BAF122029,BAF123020,BAX,BBS,BCC,BCF,BCG122006,BDB,BED,BID121027,BID121028," +
            "BID122003,BID122004,BID122005,BID123002,BID123003,BID123004,BKC,BNA,BPC,BSC,BST,BTS,BTW,BVB121034,BVB122028,BVB123025,BVS,BXH,C69,CAG," +
            "CAN,CAP,CCR,CDN,CEO,CET,CIA,CII120018,CII121006,CII121029,CII42013,CJC,CKV,CLH,CLM,CMC,CMS,CPC,CSC,CTB,CTC,CTD122015,CTG121030,CTG121031," +
            "CTG123018,CTG123019,CTG123033,CTG123034,CTP,CTT,CVN,CVT122007,CVT122008,CVT122009,CX8,D11,DAD,DAE,DC2,DDG,DHP,DHT,DIH,DL1,DNC,DNP,DP3,DPC," +
            "DS3,DST,DTC,DTD,DTG,DTK,DVG,DVM,DXP,EBS,ECI,EID,EVS,FDT,FID,GDW,GEG121022,GIC,GKM,GLH121019,GLH121026,GLT,GMA,GMX,HAD,HAT,HBS,HCC,HCT,HDA," +
            "HDG121001,HEV,HGM,HHC,HJS,HKT,HLC,HLD,HMH,HMR,HOM,HTC,HTP,HUT,HVT,ICG,IDC,IDJ,IDV,INC,INN,IPA,ITQ,IVS,KBC12006,KBC121020,KDM,KHS,KKC,KMT,KSD," +
            "KSF,KSQ,KST,KSV,KTS,KTT,L14,L18,L40,L43,L61,L62,LAS,LBE,LCD,LDP,LHC,LIG,LPB121035,LPB121036,LPB122010,LPB122011,LPB122012,LPB122013,LPB123015," +
            "LPB123016,MAC,MAS,MBG,MBS,MCC,MCF,MCO,MDC,MED,MEL,MHL,MKV,MML121021,MSC,MSN120007,MSN120008,MSN120009,MSN12001,MSN120010,MSN120011,MSN120012," +
            "MSN12002,MSN12003,MSN12005,MSN121013,MSN121014,MSN121015,MSN123008,MSN123009,MSN123010,MSN123014,MSR11808,MST,MVB,NAG,NAP,NBC,NBP,NBW,NDN,NDX,NET," +
            "NFC,NHC,NPM11907,NPM11910,NPM11911,NPM123021,NPM123022,NPM123023,NPM123024,NRC,NSH,NST,NTH,NTP,NVB,NVL122001,OCH,ONE,PBP,PCE,PCG,PCH,PCT,PDB,PEN," +
            "PGN,PGS,PGT,PHN,PIA,PIC,PJC,PLC,PMB,PMC,PMP,PMS,POT,PPE,PPP,PPS,PPT,PPY,PRC,PRE,PSC,PSD,PSE,PSI,PSW,PTD,PTI,PTS,PV2,PVB,PVC,PVG,PVI,PVS,QHD,QST,QTC," +
            "RCL,S55,S99,SAF,SBT121002,SCG,SCI,SD5,SD6,SD9,SDA,SDC,SDG,SDN,SDU,SEB,SED,SFN,SGC,SGD,SGH,SHE,SHN,SHS,SHT119008,SHT119009,SJ1,SJE,SLS,SMN,SMT,SPC," +
            "SPI,SRA,SSM,STC,STP,SVN,SZB,TA9,TAR,TBX,TC6,TDN,TDT,TET,TFC,THB,THD,THS,THT,TIG,TJC,TKC,TKG,TKU,TMB,TMC,TMX,TN1122016,TNG,TNG119007,TNG122017,TOT," +
            "TPH,TPP,TSB,TTC,TTH,TTL,TTT,TV3,TV4,TVC,TVD,TXM,UNI,V12,V21,VBA121033,VBA122001,VBB122033,VBC,VC1,VC2,VC3,VC6,VC7,VC9,VCC,VCM,VCS,VDL,VE1,VE3," +
            "VE4,VE8,VFS,VGP,VGS,VHE,VHL,VHM121024,VHM121025,VIC121003,VIC121004,VIC121005,VIC123028,VIC123029,VIF,VIG,VIT,VLA,VMC,VMS,VNC,VND122012,VND122013," +
            "VND122014,VNF,VNG122002,VNR,VNT,VNT421032,VRE12007,VSA,VSM,VTC,VTH,VTJ,VTV,VTZ,WCS,WSS,X20";
        string hnx30 = "https://bgapidatafeed.vps.com.vn/getliststockdata/BCC,BVS,CEO,DDG,DTD,DXP,HUT,IDC,L14,L18,LAS,LHC,MBS,NDN,NRC,NTP,NVB,PLC,PVC," +
            "PVS,SHS,SLS,TAR,THD,TIG,TNG,TVC,VC3,VCS,VNR";
        string upCom30 = "https://bgapidatafeed.vps.com.vn/getliststockdata/A32,AAH,AAS,ABB,ABC,ABI,ABW,ACE,ACM,ACS,ACV,AFX,AG1,AGF,AGP,AGX,AIC,ALV," +
            "AMD,AMP,AMS,ANT,APF,APL,APP,APT,ART,ASA,ATA,ATB";
        string upCom = "https://bgapidatafeed.vps.com.vn/getliststockdata/ATD,ATG,AUM,AVC,AVF,B82,BAL,BBH,BBM,BBT,BCA,BCB,BCO,BCP,BCR,BCV,BDC,BDG,BDP," +
            "BDT,BDW,BEL,BGW,BHA,BHC,BHG,BHI,BHK,BHP,BHT,BHV,BIG,BII,BIO,BLF,BLI,BLN,BLT,BLW,BM9,BMD,BMF,BMG,BMJ,BMN,BMS,BMV,BNW,BOT,BQB,BRR,BRS,BSA," +
            "BSD,BSG,BSH,BSL,BSP,BSQ,BSR,BT1,BT6,BTB,BTC,BTD,BTG,BTH,BTN,BTU,BTV,BVB,BVG,BVL,BVN,BWA,BWS,C12,C21,C22,C36,C4G,C92,CAB,CAD,CAM,CAR,CAT," +
            "CBC,CBI,CBS,CC1,CC4,CCA,CCM,CCP,CCT,CCV,CDG,CDH,CDO,CDP,CDR,CE1,CEG,CEN,CFM,CFV,CGL,CGV,CH5,CHC,CHS,CI5,CID,CIP,CK8,CKA,CKD,CLG,CLX,CMD," +
            "CMF,CMI,CMK,CMM,CMN,CMP,CMT,CMW,CNA,CNC,CNN,CNT,CPA,CPH,CPI,CQN,CQT,CSI,CST,CT3,CT6,CTA,CTN,CTW,CTX,CVC,CVH,CVP,CXH,CYC,D17,DAC,DAN,DAR," +
            "DAS,DBM,DC1,DCF,DCG,DCH,DCR,DCS,DCT,DDH,DDM,DDN,DDV,DFC,DFF,DFS,DGT,DHB,DHD,DHN,DIC,DID,DKC,DKH,DKP,DLD,DLM,DLR,DLT,DM7,DMN,DMS,DNA,DNB," +
            "DND,DNE,DNH,DNL,DNM,DNN,DNT,DNW,DNY,DOC,DOP,DP1,DP2,DPH,DPP,DPS,DRG,DRI,DSC,DSD,DSG,DSP,DSV,DT4,DTB,DTE,DTH,DTI,DTP,DTV,DUS,DVC,DVN,DVW," +
            "DWC,DWS,DXL,DZM,E12,E29,EFI,EIC,EIN,EME,EMG,EMS,EPC,EPH,FBA,FBC,FCC,FCS,FDG,FGL,FHN,FHS,FIC,FLC,FOC,FOX,FRC,FRM,FSO,FT1,FTI,FTM,G20,G36," +
            "GAB,GCB,GCF,GDA,GEE,GER,GGG,GGS,GH3,GHC,GLC,GLW,GND,GPC,GQN,GSM,GTC,GTD,GTK,GTS,GTT,GVT,H11,HAC,HAF,HAI,HAM,HAN,HAV,HBD,HBH,HC1,HC3,HCB," +
            "HCI,HD2,HD6,HD8,HDM,HDO,HDP,HDW,HEC,HEJ,HEM,HEP,HES,HFB,HFC,HFX,HGA,HGT,HGW,HHG,HHN,HHR,HIG,HIO,HJC,HKB,HKP,HLA,HLB,HLE,HLG,HLR,HLS,HLT," +
            "HLY,HMG,HMS,HNB,HND,HNF,HNI,HNM,HNP,HNR,HOT,HPB,HPD,HPH,HPI,HPM,HPP,HPT,HPW,HRB,HRT,HSA,HSI,HSM,HSP,HSV,HTE,HTH,HTM,HTR,HTT,HTU,HTW,HU3," +
            "HU4,HU6,HUG,HVA,HVG,HWS,I10,IBC,IBD,IBN,ICC,ICF,ICI,ICN,IDP,IFS,IHK,IKH,ILA,ILC,ILS,IME,IN4,IRC,ISG,ISH,IST,ITS,JOS,KAC,KCB,KCE,KGM,KHB," +
            "KHD,KHL,KHW,KIP,KLB,KLF,KLM,KSH,KTB,KTC,KTL,KTW,KVC,L12,L35,L44,L45,L63,LAI,LAW,LBC,LCC,LCM,LCS,LDW,LG9,LGM,LIC,LKW,LLM,LM3,LM7,LMC,LMH," +
            "LMI,LNC,LO5,LPT,LQN,LSG,LTC,LTG,LUT,LYF,M10,MA1,MAI,MBN,MCD,MCG,MCH,MCM,MDA,MDF,MEC,MEF,MES,MFS,MGC,MGG,MGR,MH3,MHP,MHY,MIC,MIE,MIM,MKP," +
            "MLC,MLS,MML,MNB,MND,MPC,MPT,MPY,MQB,MQN,MRF,MSR,MTA,MTB,MTC,MTG,MTH,MTL,MTM,MTP,MTS,MTV,MVC,MVN,NAB,NAC,NAS,NAU,NAW,NBE,NBT,NCG,NCS,ND2," +
            "NDC,NDF,NDP,NDT,NDW,NED,NEM,NGC,NHP,NHV,NJC,NLS,NNT,NOS,NPH,NQB,NQN,NQT,NS2,NSG,NSL,NSS,NTB,NTC,NTF,NTT,NTW,NUE,NVP,NWT,NXT,ODE,OIL,ONW," +
            "PAI,PAP,PAS,PAT,PBC,PBT,PCC,PCF,PCM,PCN,PDC,PDV,PEC,PEG,PEQ,PFL,PGB,PHH,PHP,PHS,PID,PIS,PIV,PJS,PLA,PLE,PLO,PMJ,PMT,PMW,PND,PNG,PNP,PNT," +
            "POB,POS,POV,PPH,PPI,PQN,PRO,PRT,PSB,PSG,PSL,PSN,PSP,PTE,PTG,PTH,PTO,PTP,PTT,PTV,PTX,PVA,PVE,PVH,PVL,PVM,PVO,PVR,PVV,PVX,PVY,PWA,PWS,PX1," +
            "PXA,PXC,PXI,PXL,PXM,PXS,PXT,PYU,QCC,QHW,QLD,QNC,QNS,QNT,QNU,QNW,QPH,QSP,QTP,RAT,RBC,RCC,RCD,RHN,RIC,RTB,S12,S27,S72,S74,S96,SAC,SAL,SAP," +
            "SAS,SB1,SBB,SBD,SBH,SBL,SBM,SBR,SBS,SCA,SCC,SCJ,SCL,SCO,SCY,SD1,SD2,SD3,SD4,SD7,SD8,SDB,SDD,SDE,SDJ,SDK,SDP,SDT,SDV,SDX,SDY,SEA,SEP,SGB," +
            "SGI,SGO,SGP,SGS,SHC,SHG,SHX,SID,SIG,SII,SIV,SJC,SJG,SJM,SKH,SKN,SKV,SNC,SNZ,SP2,SPB,SPD,SPH,SPV,SQC,SRB,SRT,SSF,SSG,SSH,SSN,STH,STL,STS," +
            "STT,STW,SVG,SVH,SVL,SWC,SZE,SZG,TA3,TA6,TAG,TAL,TAN,TAW,TB8,TBD,TBH,TBR,TBT,TBW,TCJ,TCK,TCW,TDB,TDF,TDI,TDS,TEC,TED,TEL,TGG,TGP,TH1,THM," +
            "THN,THP,THR,THU,THW,TID,TIE,TIN,TIS,TKA,TL4,TLI,TLP,TLT,TMG,TMW,TNB,TNM,TNP,TNS,TNW,TOP,TOS,TOW,TPS,TQN,TQW,TR1,TRS,TRT,TS3,TS4,TSD,TSG," +
            "TSJ,TST,TTB,TTD,TTG,TTJ,TTN,TTP,TTS,TTZ,TUG,TV1,TV6,TVA,TVG,TVH,TVM,TVN,TW3,UCT,UDC,UDJ,UDL,UEM,UMC,UPC,UPH,USC,USD,V11,V15,VAB,VAT,VAV," +
            "VBB,VBG,VBH,VC5,VCE,VCP,VCR,VCT,VCW,VCX,VDB,VDN,VDT,VE2,VE9,VEA,VEC,VEF,VES,VET,VFC,VFR,VGG,VGI,VGL,VGR,VGT,VGV,VHD,VHF,VHG,VHH,VIE,VIH," +
            "VIM,VIN,VIR,VIW,VKC,VKD,VKP,VLB,VLC,VLF,VLG,VLP,VLW,VMA,VMG,VMK,VMT,VNA,VNB,VNH,VNI,VNP,VNX,VNY,VNZ,VOC,VPA,VPC,VPK,VPR,VPW,VQC,VRG,VSE," +
            "VSF,VSG,VSN,VSP,VST,VT1,VTA,VTD,VTE,VTG,VTI,VTK,VTL,VTM,VTP,VTQ,VTR,VTS,VTX,VUA,VVN,VVS,VW3,VWS,VXB,VXP,VXT,WSB,WTC,WTN,X18,X26,X77,XDH," +
            "XHC,XLV,XMC,XMD,XMP,XPH,YBC,YTC";
        var client = _clientFactory.CreateClient();
        string exchangesApiUrl;
        switch (exchanges.ToLower())
        {
            case "vn30":
                exchangesApiUrl = listVN30;
                break;
            case "hose":
                exchangesApiUrl = hose;
                break;
            case "hnx":
                exchangesApiUrl = hnx;
                break;
            case "hnx30":
                exchangesApiUrl = hnx30;
                break;
            default:
                exchangesApiUrl = listVN30;
                break;
        }
        // upcom exchanges -> get data from 2 api -> concat 2 data -> return 
        if (exchanges.ToLower().Equals("upcom"))
        {
            var upcom30Response = await client.GetAsync(upCom30);
            if (upcom30Response.IsSuccessStatusCode)
            {
                var upcom30Content = await upcom30Response.Content.ReadAsStringAsync();
                var upcom30Stocks = JsonConvert.DeserializeObject<Stock[]>(upcom30Content);
                if (upcom30Stocks == null || upcom30Stocks.Length == 0)
                {
                    return BadRequest("Exchanges code not found");
                }
                var upcomResponse = await client.GetAsync(upCom);
                var upcomContent = await upcomResponse.Content.ReadAsStringAsync();
                var upcomStocks = JsonConvert.DeserializeObject<Stock[]>(upcomContent);
                if (upcomStocks == null || upcomStocks.Length == 0)
                {
                    return BadRequest("Exchanges code not found");
                }
                return Ok(upcom30Stocks.Concat(upcomStocks).ToArray());
            }
            else
            {
                return StatusCode(500, "Failed to get data from the API.");
            }
        }
        // other exchanges
        else
        {
            var exchangesDataResponse = await client.GetAsync(exchangesApiUrl);
            if (exchangesDataResponse.IsSuccessStatusCode)
            {
                var exchangesContent = await exchangesDataResponse.Content.ReadAsStringAsync();
                var exchangesStockData = JsonConvert.DeserializeObject<Stock[]>(exchangesContent);
                if (exchangesStockData == null || exchangesStockData.Length == 0)
                {
                    return BadRequest("Exchanges code not found");
                }
                return Ok(exchangesStockData);
            }
            else
            {
                return StatusCode((int)exchangesDataResponse.StatusCode, "Failed to get data from the API.");
            }
        }
    }
    private StocksOverTimeResponse[] MapToSOToSOR(StocksOverTime stocksOverTime)
    {
        var t = stocksOverTime.t;
        var c = stocksOverTime.c;
        var o = stocksOverTime.o;
        var h = stocksOverTime.h;
        var l = stocksOverTime.l;
        var v = stocksOverTime.v;
        List<StocksOverTimeResponse> stockDataList = new List<StocksOverTimeResponse>();
        for (int i = 0; i < Math.Min(t.Length, Math.Min(c.Length, Math.Min(o.Length, Math.Min(h.Length, l.Length)))); i++)
        {
            StocksOverTimeResponse stockData = new StocksOverTimeResponse
            {
                Time = UnixTimeStampToDateTime(t[i]),
                Close = c[i],
                Open = o[i],
                High = h[i],
                Low = l[i],
                Volume = v[i]
            };
            stockDataList.Add(stockData);
        }
        StocksOverTimeResponse[] stockDataArray = stockDataList.ToArray();
        return stockDataArray;
    }
}
