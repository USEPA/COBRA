import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import * as Highcharts from 'highcharts';
import patternFill from 'highcharts/modules/pattern-fill';

// Initialize pattern-fill module
patternFill(Highcharts);

@Component({
  selector: 'app-dac-chart',
  templateUrl: './dac-chart.component.html',
  styleUrls: ['./dac-chart.component.scss'],
})
export class DacChartComponent implements OnChanges {
  Highcharts: typeof Highcharts = Highcharts;
  @Input() populationData: any;

  chartOptions: Highcharts.Options;
  updateFlag = false; // Flag to trigger chart updates
  private chart: Highcharts.Chart | any = null;

  // Chart callback to get chart instance
  chartCallback = (chart: Highcharts.Chart) => {
    this.chart = chart;
  };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['populationData'] && this.populationData) {

      const hasBenefits = this.populationData.benefits && !this.populationData.disbenefits;
      const hasDisbenefits = this.populationData.disbenefits && !this.populationData.benefits;
      const hasBoth = this.populationData.benefits && this.populationData.disbenefits;
      const populationData = this.populationData;
      

      this.chartOptions = {
        chart: {
            type: 'column',
            custom: {
              populationData: this.populationData // ✅ Pass populationData here
          },
        },
        exporting: {
            enabled: false,
        },
        title: {
            text: null,
        },
        xAxis: {
            categories: hasBenefits ? ['Benefits', 'Population'] :
                       hasDisbenefits ? ['Disbenefits', 'Population'] :
                       ['Benefits', 'Disbenefits', 'Population'],
            title: {
                text: null,
            },
        },
        yAxis: {
            visible: false,
            labels: {
                enabled: false,
            },
            gridLineWidth: 0,
            title: {
                text: null,
            },
        },
        legend: {
            align: 'center',
            verticalAlign: 'bottom',
            layout: 'horizontal',
            y: 10,
        },
        tooltip: {
          formatter: function () {
              const chart = this.series.chart;
              const xIndex = this.point.index;
              let total = 0;

              // Sum up all values at this xIndex across series
              chart.series.forEach((series) => {
                  const point = series.data[xIndex];
                  if (point && point.y !== null) {
                      total += point.y;
                  }
              });

              if (this.point.category === 'Population') {
                  return `<b>Total ${this.point.category}: ${populationData.totalPopulation?.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 0 })}</b><br/><br/>
                          <b>${this.y.toFixed(2)}%</b> of the <b>${this.point.category.toLowerCase()}</b> is
                          <b>${this.series.name.toLowerCase().includes("outside") ? "outside your filtered communities of interest" : "within your communities of interest"}</b>.<br/>`;
              } 

              const totalValue = this.point.category.toLowerCase().includes('dis') 
                  ? populationData.totalDisbenefitsValue 
                  : populationData.totalBenefitsValue;

              return `<b>Total ${this.point.category}: $${totalValue?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</b><br/>
              <br/>
                      <b>${this.y.toFixed(2)}%</b> of <b>${this.point.category.toLowerCase()}</b> are going towards <b>${this.series.name.toLowerCase().includes("outside") ? "populations outside your filtered communities of interest" : "communities of interest"}</b>.`;
          }
      },
       
        plotOptions: {
            column: {
                stacking: 'normal',
                dataLabels: {
                    enabled: true,
                    formatter: function () {
                      if (this.y > 0 && this.y < 1) {
                        return '<1%';
                      }
                      if (Math.round(this.y) === 100 && this.y < 100) {
                        return '>99%';
                      }
                      return `${Math.round(this.y)}%`;
                    },
                },
            },
        },
        credits: {
            enabled: false,
        },
        series: hasBenefits
            ? [
                  {
                      type: 'column',
                      name: 'Population outside COI',
                      data: [
                          this.populationData.benefitsGoingtoNonDAC,
                          this.populationData.nonDacPercentPop,
                      ],
                      stack: 'group',
                      showInLegend: true,
                       color: {
                          patternIndex: 0,
                      } as any,
                  },
                  {
                      type: 'column',
                      name: 'Communities of Interest (COI)',
                      data: [
                          this.populationData.benefitsGoingtoDAC,
                          this.populationData.dacPercentPop,
                      ],
                      stack: 'group',
                      showInLegend: true,
                     color: '#005ea2',
                  },
              ]
            : hasDisbenefits
            ? [
                  {
                      type: 'column',
                      name: 'Population outside COI',
                      data: [
                          this.populationData.disbenefitsGoingtoNonDAC,
                          this.populationData.nonDacPercentPop,
                      ],
                      stack: 'group',
                      showInLegend: true,
                       color: {
                          patternIndex: 0,
                      } as any,
                  },
                  {
                      type: 'column',
                      name: 'Communities of Interest (COI)',
                      data: [
                          this.populationData.disbenefitsGoingtoDAC,
                          this.populationData.dacPercentPop,
                      ],
                      stack: 'group',
                      showInLegend: true,
                     color: '#005ea2',
                  },
              ]
              //both benefits and disbenefits
            : [
                  {
                      type: 'column',
                      name: 'Population outside COI',
                      data: [
                          this.populationData.benefitsGoingtoNonDAC,
                          this.populationData.disbenefitsGoingtoNonDAC,
                          this.populationData.nonDacPercentPop,
                      ],
                      stack: 'group',
                      showInLegend: true,
                       color: {
                          patternIndex: 0,
                      } as any,
                  },
                  {
                      type: 'column',
                      name: 'Communities of Interest (COI)',
                      data: [
                          this.populationData.benefitsGoingtoDAC,
                          this.populationData.disbenefitsGoingtoDAC,
                          this.populationData.dacPercentPop,
                      ],
                      stack: 'group',
                      showInLegend: true,
                     color: '#005ea2',
                  },
              ],
    } as any;
      if (this.chart) {
        (this.chartOptions as any).populationData = this.populationData;
        this.updateChartData();
        
      } else {
        this.initializeChartOptions();
        this.updateFlag = true; // Trigger re-render
      }
    }
    
  }

  // Initialize the chart options once
  constructor() {
   
  }
  private updateChartData(): void {
    if (this.chart) {
        const nonDACSeries = this.chart.series[0];
        const dacSeries = this.chart.series[1];

        if (this.populationData.benefits && !this.populationData.disbenefits) {
            // All benefits
            nonDACSeries.setData([
                this.populationData.benefitsGoingtoNonDAC,
                this.populationData.nonDacPercentPop
            ]);

            dacSeries.setData([
                this.populationData.benefitsGoingtoDAC,
                this.populationData.dacPercentPop
            ]);

        } else if (this.populationData.disbenefits && !this.populationData.benefits) {
            // All disbenefits
            nonDACSeries.setData([
                this.populationData.disbenefitsGoingtoNonDAC,
                this.populationData.nonDacPercentPop
            ]);

            dacSeries.setData([
                this.populationData.disbenefitsGoingtoDAC,
                this.populationData.dacPercentPop
            ]);

        } else {
            // Both benefits and disbenefits
            nonDACSeries.setData([
                this.populationData.benefitsGoingtoNonDAC,
                this.populationData.disbenefitsGoingtoNonDAC,
                this.populationData.nonDacPercentPop
            ]);

            dacSeries.setData([
                this.populationData.benefitsGoingtoDAC,
                this.populationData.disbenefitsGoingtoDAC,
                this.populationData.dacPercentPop
            ]);
        }

        // Trigger reflow to update the chart display
        this.chart.reflow();

    } else {
        // Chart is not initialized; set up options and trigger render
        this.initializeChartOptions();
        this.updateFlag = true; // Trigger chart re-render
    }
}

tooltipFormatter(this: Highcharts.TooltipFormatterContextObject): string {
  const chart = this.series.chart;
  const xIndex = this.point.index;
  let total = 0;

  // Sum up all values at this xIndex across series
  chart.series.forEach((series) => {
      const point = series.data[xIndex];
      if (point && point.y !== null) {
          total += point.y;
      }
  });

  // ✅ Now `this` refers to the Angular component (because of `bind(this)`)
  const populationData = (chart.options as any).populationData || {};

  if (this.point.category === 'Population') {
      return `<b>Total ${this.point.category}: ${populationData.totalPopulation?.toLocaleString()}</b><br/>
              <b>${this.y.toFixed(2)}%</b> of the <b>${this.point.category.toLowerCase()}</b> is considered 
              <b>${this.series.name.toLowerCase().includes("non") ? "non-disadvantaged" : "disadvantaged"}</b>.<br/>`;
  } 

  const totalValue = this.point.category.toLowerCase().includes('dis') 
      ? populationData.totalDisbenefitsValue 
      : populationData.totalBenefitsValue;

  return `<b>Total ${this.point.category}: $${totalValue?.toFixed(2).toLocaleString()}</b><br/>
          <b>${this.y.toFixed(2)}%</b> of <b>${this.point.category.toLowerCase()}</b> are going towards <b>${this.series.name.toLowerCase()}</b>.<br/>`
}

  private initializeChartOptions(): void {
    if (this.populationData.benefits && !this.populationData.disbenefits) {
    
      (this.chartOptions as any).populationData = this.populationData;
      this.chartOptions.series = [
        {
          type: 'column',
          name: 'Population outside COI',
          data: [
            this.populationData.benefitsGoingtoNonDAC,
            this.populationData.nonDacPercentPop,
          ],
          //data: [20, 10],
          stack: 'group',
          showInLegend: true,
           color: {
                          patternIndex: 0,
                      } as any, //darker version of EPA blue
        },
        {
          type: 'column',
          name: 'Communities of Interest (COI)',
          data: [
            this.populationData.benefitsGoingtoDAC,
            this.populationData.dacPercentPop,
          ],
          //data: [10, 20],
          stack: 'group',
          showInLegend: true,
          color: {
            patternIndex: 0, //9 is like a light teal lines, 7 interesting dark greenish circles,
          } as any,
        },
      ];
      //all disbenefits
    } else if (
      this.populationData.disbenefits &&
      !this.populationData.benefits
    ) {
      console.log("ALL DISBENEFITS");
      this.chartOptions.series = [
        {
          type: 'column',
          name: 'Population outside COI',
          data: [
            this.populationData.disbenefitsGoingtoNonDAC,
            this.populationData.nonDacPercentPop,
          ],
          //data: [20, 10],
          stack: 'group',
          showInLegend: true,
           color: {
                          patternIndex: 0,
                      } as any, //darker version of EPA blue
        },
        {
          type: 'column',
          name: 'Communities of Interest (COI)',
          data: [
            this.populationData.disbenefitsGoingtoDAC,
            this.populationData.dacPercentPop,
          ],
          //data: [10, 20],
          stack: 'group',
          showInLegend: true,
          color: {
            patternIndex: 0, //9 is like a light teal lines, 7 interesting dark greenish circles,
          } as any,
        },
      ];


    } else {
            //there are both benefits and disbenefits
           console.log("BOTH BENEFITS AND DISBENEFITS")
      this.chartOptions.series = [
        {
          type: 'column',
          name: 'Population outside COI',
          data: [
            this.populationData.benefitsGoingtoNonDAC,
            this.populationData.disbenefitsGoingtoNonDAC,
            this.populationData.nonDacPercentPop,
          ],
          //data: [10,20, 10],
          stack: 'group',
          showInLegend: true,
           color: {
                          patternIndex: 0,
                      } as any, //darker version of EPA blue
        },
        {
          type: 'column',
          name: 'Communities of Interest (COI)',
          data: [
            this.populationData.benefitsGoingtoDAC,
            this.populationData.disbenefitsGoingtoDAC,
            this.populationData.dacPercentPop,
          ],
          //data: [20,10, 20],
          stack: 'group',
          showInLegend: true,
          color: {
            patternIndex: 0, //9 is like a light teal lines, 7 interesting dark greenish circles,
          } as any,
        },
      ];
    }
  }
}
