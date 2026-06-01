package com.ceb.reporting.phvvalidation.service;

import com.ceb.reporting.phvvalidation.cli.CommandLineOptions;
import com.ceb.reporting.phvvalidation.model.PhvValidationRow;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import net.sf.jasperreports.engine.JasperCompileManager;
import net.sf.jasperreports.engine.JasperExportManager;
import net.sf.jasperreports.engine.JasperFillManager;
import net.sf.jasperreports.engine.JasperPrint;
import net.sf.jasperreports.engine.JasperReport;
import net.sf.jasperreports.engine.data.JRBeanCollectionDataSource;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class PhvValidationReportService {
    private final ObjectMapper objectMapper = new ObjectMapper();

    public void generate(CommandLineOptions options) throws Exception {
        Path inputPath = Path.of(options.inputJsonPath());
        Path outputPath = Path.of(options.outputPdfPath());
        Path templatePath = Path.of(options.templatePath());

        List<PhvValidationRow> rows = readRows(inputPath);
        if (rows.isEmpty()) {
            throw new IllegalArgumentException("The input JSON file does not contain any PHV validation rows.");
        }

        JasperReport report = JasperCompileManager.compileReport(templatePath.toString());

        Map<String, Object> parameters = new HashMap<>();
        parameters.put("costctr", options.costCenterLabel());
        parameters.put("repYear", options.reportYear());
        parameters.put("repMonth", options.reportMonth());

        JasperPrint jasperPrint = JasperFillManager.fillReport(
                report,
                parameters,
                new JRBeanCollectionDataSource(rows, false));

        Files.createDirectories(outputPath.getParent());
        JasperExportManager.exportReportToPdfFile(jasperPrint, outputPath.toString());

        if (!looksLikePdf(outputPath)) {
            throw new IllegalStateException("Generated file does not appear to be a valid PDF: " + outputPath);
        }
    }

    private List<PhvValidationRow> readRows(Path inputPath) throws IOException {
        return objectMapper.readValue(
                Files.readString(inputPath),
                new TypeReference<List<PhvValidationRow>>() {
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
}