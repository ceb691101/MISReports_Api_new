package com.ceb.reporting.areatrialbalance;

import com.ceb.reporting.areatrialbalance.cli.CommandLineOptions;
import com.ceb.reporting.areatrialbalance.cli.CommandLineParser;
import com.ceb.reporting.areatrialbalance.service.AreaTrialBalanceReportService;

public class Main {
    public static void main(String[] args) {
        try {
            CommandLineOptions options = CommandLineParser.parse(args);
            new AreaTrialBalanceReportService().generate(options);
        } catch (Exception ex) {
            System.err.println(ex.getMessage());
            ex.printStackTrace(System.err);
            System.exit(1);
        }
    }
}
