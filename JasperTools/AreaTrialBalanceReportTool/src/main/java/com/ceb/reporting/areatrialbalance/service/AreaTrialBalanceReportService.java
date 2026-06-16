package com.ceb.reporting.areatrialbalance.service;

import com.ceb.reporting.areatrialbalance.cli.CommandLineOptions;
import com.ceb.reporting.areatrialbalance.model.AreaTrialBalanceRow;
import com.ceb.reporting.areatrialbalance.util.ReadPath;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import net.sf.jasperreports.engine.data.JRBeanCollectionDataSource;
import net.sf.jasperreports.engine.JasperCompileManager;
import net.sf.jasperreports.engine.JasperExportManager;
import net.sf.jasperreports.engine.JasperFillManager;
import net.sf.jasperreports.engine.JasperPrint;
import net.sf.jasperreports.engine.JasperReport;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class AreaTrialBalanceReportService {
    private final ObjectMapper objectMapper = new ObjectMapper();

    public void generate(CommandLineOptions options) throws Exception {
        Path inputPath = Path.of(options.inputJsonPath());
        Path outputPath = Path.of(options.outputPdfPath());
        Path templatePath = resolveTemplatePath(options.templatePath());

        List<AreaTrialBalanceRow> rows = readRows(inputPath);
        if (rows.isEmpty()) {
            throw new IllegalArgumentException("The input JSON file does not contain any trial balance rows.");
        }

        JasperReport report = JasperCompileManager.compileReport(templatePath.toString());

        Map<String, Object> parameters = new HashMap<>();
        parameters.put("@compId", options.compId());
        parameters.put("@repyear", options.reportYear());
        parameters.put("@repmonth", options.reportMonth());

        JasperPrint jasperPrint = JasperFillManager.fillReport(
                report,
                parameters,
                new JRBeanCollectionDataSource(rows, false));

        Files.createDirectories(outputPath.getParent());
        String outStr = outputPath.toString().toLowerCase();
        if (outStr.endsWith(".csv")) {
            net.sf.jasperreports.engine.export.JRCsvExporter exporter = new net.sf.jasperreports.engine.export.JRCsvExporter();
            exporter.setExporterInput(new net.sf.jasperreports.export.SimpleExporterInput(jasperPrint));
            exporter.setExporterOutput(new net.sf.jasperreports.export.SimpleWriterExporterOutput(outputPath.toFile()));
            exporter.exportReport();
        } else {
            JasperExportManager.exportReportToPdfFile(jasperPrint, outputPath.toString());
            if (!looksLikePdf(outputPath)) {
                throw new IllegalStateException("Generated file does not appear to be a valid PDF: " + outputPath);
            }
        }
    }

    private List<AreaTrialBalanceRow> readRows(Path inputPath) throws IOException {
        return objectMapper.readValue(
                Files.readString(inputPath),
                new TypeReference<List<AreaTrialBalanceRow>>() {
                });
    }

    private boolean looksLikePdf(Path outputPath) throws IOException {
        byte[] firstBytes = Files.readAllBytes(outputPath);
        return firstBytes.length >= 5
                && firstBytes[0] == '%'
                && firstBytes[1] == 'P'
                && firstBytes[2] == 'D'
                && firstBytes[3] == 'F'
                && firstBytes[4] == '-';
    }

    private Path resolveTemplatePath(String fallbackTemplatePath) throws IOException {
        String configuredPath = new ReadPath().getPath();
        if (configuredPath != null && !configuredPath.isBlank()) {
            return Path.of(configuredPath);
        }
        return Path.of(fallbackTemplatePath);
    }
}
