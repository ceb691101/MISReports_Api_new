package com.ceb.reporting.solarpending;

import com.ceb.reporting.solarpending.cli.CommandLineOptions;
import com.ceb.reporting.solarpending.cli.CommandLineParser;
import com.ceb.reporting.solarpending.service.SolarPendingJobsReportService;

public class Main {
    public static void main(String[] args) {
        try {
            CommandLineOptions options = CommandLineParser.parse(args);
            new SolarPendingJobsReportService().generate(options);
        } catch (Exception ex) {
            System.err.println(ex.getMessage());
            ex.printStackTrace(System.err);
            System.exit(1);
        }
    }
}
