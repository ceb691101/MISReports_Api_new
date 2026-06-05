package com.ceb.reporting.ccapplication;

import com.ceb.reporting.ccapplication.cli.CommandLineOptions;
import com.ceb.reporting.ccapplication.cli.CommandLineParser;
import com.ceb.reporting.ccapplication.service.CcApplicationReportService;

public class Main {
    public static void main(String[] args) {
        try {
            CommandLineOptions options = CommandLineParser.parse(args);
            new CcApplicationReportService().generate(options);
        } catch (Exception ex) {
            System.err.println(ex.getMessage());
            ex.printStackTrace(System.err);
            System.exit(1);
        }
    }
}
