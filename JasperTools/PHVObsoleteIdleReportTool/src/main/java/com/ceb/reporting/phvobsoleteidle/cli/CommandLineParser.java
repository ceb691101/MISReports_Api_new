package com.ceb.reporting.phvobsoleteidle.cli;

import java.util.HashMap;
import java.util.Map;

public final class CommandLineParser {
    private CommandLineParser() {
    }

    public static CommandLineOptions parse(String[] args) {
        Map<String, String> values = new HashMap<>();
        for (int i = 0; i < args.length; i++) {
            String arg = args[i];
            if (arg.startsWith("--")) {
                String key = arg.substring(2).toLowerCase();
                if (i + 1 >= args.length || args[i + 1].startsWith("--")) {
                    throw new IllegalArgumentException("Missing value for argument: " + arg);
                }
                values.put(key, args[++i]);
            }
        }

        String input = require(values, "input");
        String output = require(values, "output");
        String template = require(values, "template");
        String costctr = require(values, "costctr");
        String whcode = require(values, "whcode");
        int repYear = parseInt(require(values, "repyear"), "repyear");
        int repMonth = parseInt(require(values, "repmonth"), "repmonth");

        return new CommandLineOptions(input, output, template, costctr, whcode, repYear, repMonth);
    }

    private static String require(Map<String, String> values, String key) {
        String value = values.get(key);
        if (value == null || value.trim().isEmpty()) {
            throw new IllegalArgumentException("Missing required argument: --" + key);
        }
        return value;
    }

    private static int parseInt(String value, String key) {
        try {
            return Integer.parseInt(value);
        } catch (NumberFormatException ex) {
            throw new IllegalArgumentException("Argument --" + key + " must be a valid integer.", ex);
        }
    }
}