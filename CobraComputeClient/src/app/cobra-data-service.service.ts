import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { Token } from './Token';
import { GlobalsService } from './globals.service';

@Injectable({
  providedIn: 'root',
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
  //public serverendpoint = 'https://cobradevapi.app.cloud.gov/api/';
  public serverendpoint = 'https://cobradevapi.app.cloud.gov/api/';
  //public serverendpoint = 'http://localhost/CobraComputeAPI/api/';
  //public serverendpoint = 'https://cobraprotoapi.app.cloud.gov/api/';

  private tokenUrl = this.serverendpoint + 'token'; // URL to web api
  private resultUrl = this.serverendpoint + 'Result'; // URL to web api
  private datadictUrl = this.serverendpoint + 'datadictionary'; // URL to web api
  public emissionsUrl = this.serverendpoint + 'SummarizedControlEmissions'; // URL to web api
  private emissionsupdateUrl = this.serverendpoint + 'EmissionsUpdate'; // URL to web api
  private emissionsbatchupdateUrl =
    this.serverendpoint + 'EmissionsBatchUpdate'; // URL to web api
  private sumemissionsUrl = this.serverendpoint + 'SummarizedEmissions';
  private resultExportUrl = this.serverendpoint + 'ExcelExport';
  private SummaryExportUrl = this.serverendpoint + 'SummaryExcel';
  private resetUrl = this.serverendpoint + 'reset'; // URL to web api
  private QueueUrl = this.serverendpoint + 'Queue/'; // URL to web api

  constructor(private http: HttpClient, private globals: GlobalsService) {}

  createRandomNumber() {
    return Math.floor(10000000 + Math.random() * 90000000);
  }

  getQueue(atoken): Observable<any> {
    return this.http.get<any>(this.QueueUrl + atoken);
  }

  getToken(): Observable<Token> {
    return this.http.get<Token>(
      this.tokenUrl + '?' + this.createRandomNumber()
    );
  }

  getResults(filterval: any, rate: any): Observable<any[]> {
    if (filterval && filterval != '00') {
      return this.http.get<any[]>(
        this.resultUrl +
          '/' +
          this.globals.getToken() +
          '/' +
          filterval +
          '?discountrate=' +
          rate
      );
    } else {
      return this.http.get<any[]>(
        this.resultUrl + '/' + this.globals.getToken() + '?discountrate=' + rate
      );
    }
  }

  getDataDictionary_State(): Observable<any[]> {
    return this.http.get<any[]>(this.datadictUrl + '?which=state');
  }

  getDataDictionary_Tiers(): Observable<any[]> {
    return this.http.get<any[]>(this.datadictUrl + '?which=tiers');
  }

  getEmissionsData(fipscodes, tiers): Observable<any[]> {
    let params = new HttpParams({
      fromObject: {
        token: this.globals.getToken(),
        fipscodes: fipscodes,
        tiers: tiers,
      },
    });
    return this.http.get<any[]>(this.emissionsUrl, { params: params });
  }

  resetEmissionsData(): Observable<any[]> {
    let body = {
      token: this.globals.getToken(),
    };
    return this.http.post<any>(this.resetUrl, body);
  }

  updateEmissionsData(results: any): Observable<any[]> {
    let body = {
      operationalMode: this.globals.getMode(),
      spec: {
        token: this.globals.getToken(),
        fipscodes: results['fipscodes'],
        tiers: results['tierselection'][0],
      },
      payload: {
        NOx: results['NOx'],
        SO2: results['SO2'],
        NH3: results['NH3'],
        PM25: results['PM25'],
        VOC: results['VOC'],
      },
    };
    return this.http.post<any[]>(this.emissionsupdateUrl, body);
  }

  batchupdateEmissionsData(results: [], mode: string): Observable<any[]> {
    let data = [];
    results.forEach((element) => {
      let body = {
        operationalMode: this.globals.getMode(),
        spec: {
          token: this.globals.getToken(),
          fipscodes: element['fipscodes'],
          tiers: element['tierselection'][0],
        },
        payload: {
          NOx: element['NOx'],
          SO2: element['SO2'],
          NH3: element['NH3'],
          PM25: element['PM25'],
          VOC: element['VOC'],
        },
      };
      data.push(body);
    });
    return this.http.post<any[]>(this.emissionsbatchupdateUrl, data);
  }

  getSummarizedEmissionsData(fipscodes, tiers): Observable<any[]> {
    let params = new HttpParams({
      fromObject: {
        token: this.globals.getToken(),
        fipscodes: fipscodes,
        tiers: tiers,
      },
    });
    return this.http.get<any[]>(this.sumemissionsUrl, { params: params });
  }

  getReviewData(fipscodes, tiers): Observable<any[]> {
    let params = new HttpParams({
      fromObject: {
        token: this.globals.getToken(),
        fipscodes: fipscodes,
        tiers: tiers,
      },
    });
    return this.http.get<any[]>(this.emissionsUrl, { params: params });
  }

  updateReviewData(results): Observable<any[]> {
    let body = {
      spec: {
        token: this.globals.getToken(),
        fipscodes: results['fipscodes'],
        tiers: results['tierselection'][0],
      },
      payload: {
        NOx: results['NOx'],
        SO2: results['SO2'],
        NH3: results['NH3'],
        PM25: results['PM25'],
        VOC: results['VOC'],
      },
    };
    return this.http.post<any[]>(this.emissionsupdateUrl, body);
  }

  public exportAllResults(kind: any, rate: any) {
    return this.http.get(
      this.resultExportUrl +
        '/' +
        this.globals.getToken() +
        '/' +
        kind +
        '?discountrate=' +
        rate,
      { responseType: 'blob' }
    );
  }

  public exportSummary(filter: any, rate: any) {
    return this.http.get(
      this.SummaryExportUrl +
        '/' +
        this.globals.getToken() +
        '/' +
        filter +
        '?discountrate=' +
        rate,
      { responseType: 'blob' }
    );
  }
}
