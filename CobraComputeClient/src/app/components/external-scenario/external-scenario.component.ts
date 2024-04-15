import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CobraDataService } from 'src/app/cobra-data-service.service';
import { GlobalsService } from 'src/app/globals.service';

@Component({
  selector: 'app-external-scenario',
  templateUrl: './external-scenario.component.html',
  styleUrls: ['./external-scenario.component.scss']
})
export class ExternalScenarioComponent implements OnInit {
  token: string = '';

  constructor(private cobraDataService: CobraDataService, private global: GlobalsService, private route: ActivatedRoute, private router: Router,
  ) { }
  
  ngOnInit(): void {
    const routeParams = this.route.snapshot.paramMap;
    
    this.token = routeParams.get('token');

    //store token;
    this.global.setToken(this.token);
    this.global.setMode("AVERT");

    //get queue
    this.cobraDataService.getQueue(this.token).subscribe(
      data => {
        //sets queue, also sets mode to AVERT
        this.global.setQueue(data);
        this.router.navigateByUrl('/');
      }
    );


  }

}
