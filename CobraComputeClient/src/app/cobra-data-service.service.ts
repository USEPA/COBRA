import { Injectable } from '@angular/core';
import { HttpClientModule, HttpParams }    from '@angular/common/http';
import { HttpClient, HttpRequest, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { Token } from './Token';
import { catchError, map, tap } from 'rxjs/operators';
import { GlobalsService } from './globals.service';
// import { RequestOptions } from '@angular/http';
import { JSDocTagName } from '@angular/compiler/src/output/output_ast';
import { BehaviorSubject } from 'rxjs/Rx'
import { saveAs } from 'file-saver';


@Injectable({
  providedIn: 'root'
})
export class CobraDataService {
  
  public dataSource = new BehaviorSubject<any>([]);
  
  currentData = this.dataSource.asObservable();
  changeData(data: any) {
    this.dataSource.next(data);
  }
  public stateCountyDataSource = new BehaviorSubject<any>([]);
  stateCountyData = this.stateCountyDataSource.asObservable();
  
  changeSCData(data: any) {
    this.stateCountyDataSource.next(data);
  }

  //public serverendpoint = 'https://cobraapi.app.cloud.gov/api/';
  public serverendpoint = 'https://cobradevapi.app.cloud.gov/api/';
  //private serverendpoint = 'http://localhost/CobraComputeAPI/api/';
  

  private tokenUrl = this.serverendpoint + 'token';  // URL to web api
  private resultUrl = this.serverendpoint + 'Result';  // URL to web api
  private datadictUrl = this.serverendpoint + 'datadictionary';  // URL to web api
  public emissionsUrl = this.serverendpoint + 'SummarizedControlEmissions';  // URL to web api
  private emissionsupdateUrl = this.serverendpoint + 'EmissionsUpdate';  // URL to web api
  private sumemissionsUrl = this.serverendpoint + 'SummarizedEmissions'; 
  private resultExportUrl = this.serverendpoint + 'ExcelExport'; 
  private SummaryExportUrl = this.serverendpoint + 'SummaryExcel'; 

  constructor( private http: HttpClient, private globals: GlobalsService) { }
  
  getToken (): Observable<Token> {
    return this.http.get<Token>(this.tokenUrl);
  }

  // getResults(filterval): Observable<any[]> {
  //   if (filterval && filterval!='00') {
  //     return this.http.get<any[]>(this.resultUrl + "/" + this.globals.getToken()+"/" + filterval);
  //   }
  //   else {
  //     return this.http.get<any[]>(this.resultUrl + "/" + this.globals.getToken());
  //   }
  // }

  getResults(filterval: any, rate: any): Observable<any[]> {
    if (filterval && filterval!='00') {
      return this.http.get<any[]>(this.resultUrl + "/" + this.globals.getToken()+"/" + filterval +"?discountrate="+rate);
    }
    else {
      return this.http.get<any[]>(this.resultUrl + "/" + this.globals.getToken()+"?discountrate="+rate);
    }
  }

  getDataDictionary_State (): Observable<any[]> {
    return this.http.get<any[]>(this.datadictUrl+"?which=state");
  }

  getDataDictionary_Tiers(): Observable<any[]> {
    return this.http.get<any[]>(this.datadictUrl + "?which=tiers");
  }

  getEmissionsData(fipscodes, tiers): Observable<any[]> {
    let params = new HttpParams({
      fromObject : {
        'token' : this.globals.getToken(),
        'fipscodes' : fipscodes,
        'tiers' : tiers
      }
    });
    return this.http.get<any[]>(this.emissionsUrl,{ params: params });
  }

  updateEmissionsData(results): Observable<any[]> {
    let body = {
      spec : {
        'token' : this.globals.getToken(),
        'fipscodes': results['fipscodes'],
        'tiers' : results['tierselection'][0],
      },
      payload: {
        'NO2': results['NO2'],
        'SO2': results['SO2'],
        'NH3': results['NH3'],
        'PM25': results['PM25'],
        'VOC': results['VOC']
      }
    };
    return this.http.post<any[]>(this.emissionsupdateUrl, body);
  }

  getSummarizedEmissionsData(fipscodes, tiers): Observable<any[]> {
    let params = new HttpParams({
      fromObject: {
        'token': this.globals.getToken(),
        'fipscodes': fipscodes,
        'tiers': tiers
      }
    });
    return this.http.get<any[]>(this.sumemissionsUrl, { params: params });
  }

  getReviewData(fipscodes, tiers): Observable<any[]> {
    let params = new HttpParams({
      fromObject: {
        'token': this.globals.getToken(),
        'fipscodes': fipscodes,
        'tiers': tiers
      }
    });
    return this.http.get<any[]>(this.emissionsUrl, { params: params });
  }

  updateReviewData(results): Observable<any[]> {
    let body = {
      spec: {
        'token': this.globals.getToken(),
        'fipscodes': results['fipscodes'],
        'tiers': results['tierselection'][0],
      },
      payload: {
        'NO2': results['NO2'],
        'SO2': results['SO2'],
        'NH3': results['NH3'],
        'PM25': results['PM25'],
        'VOC': results['VOC']
      }
    };
    return this.http.post<any[]>(this.emissionsupdateUrl, body);
  }

  export_results(kind: any, rate: any) {
    return this.http.get(this.resultExportUrl + "/" +this.globals.getToken()+"/" + kind + "?discountrate=" + rate, {responseType: 'blob'});
  }
  
  public ExcelExport(kind: any, rate: any, callback = null) {
    var filename = "ExcelResultReport.xls";
    if (kind == 'base') { filename = "BaselineEmissions.xls" };
    if (kind == 'control') { filename = "ControlEmissions.xls" };

    this.export_results(kind, rate).subscribe(data => {
      saveAs(data, filename);
      if (callback) callback();
    });
  }

  public SummaryExcelExport(filter: any, rate: any) {
    var filename = "SummaryExcelReport.xls";
    this.http.get(this.SummaryExportUrl + "/" + this.globals.getToken() + "/" + filter + "?discountrate=" + rate, { responseType: 'blob' }).subscribe(data => {
      saveAs(data, filename);
    });
  }
 
}
