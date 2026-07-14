package com.ceb.reporting.phvobsoleteidle.service;

import com.ceb.reporting.phvobsoleteidle.cli.CommandLineOptions;
import com.ceb.reporting.phvobsoleteidle.model.PhvObsoleteIdleRow;
import com.ceb.reporting.phvobsoleteidle.util.ReadPath;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import net.sf.jasperreports.engine.data.JRBeanCollectionDataSource;
import net.sf.jasperreports.engine.JasperCompileManager;
import net.sf.jasperreports.engine.JasperExportManager;
import net.sf.jasperreports.engine.JasperFillManager;
import net.sf.jasperreports.engine.JasperPrint;
import net.sf.jasperreports.engine.JasperReport;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.OffsetDateTime;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class PhvObsoleteIdleReportService {
    private final ObjectMapper objectMapper = new ObjectMapper();

    public void generate(CommandLineOptions options) throws Exception {
        Path inputPath = Paths.get(options.inputJsonPath());
        Path outputPath = Paths.get(options.outputPdfPath());
        Path templatePath = resolveTemplatePath(options.templatePath());

        List<PhvObsoleteIdleRow> rows = readRows(inputPath);
        if (rows.isEmpty()) {
            throw new IllegalArgumentException("The input JSON file does not contain any obsolete/idle rows.");
        }

        for (PhvObsoleteIdleRow row : rows) {
            row.setPhvDate(formatDate(row.getPhvDate()));
        }

        JasperReport report = JasperCompileManager.compileReport(templatePath.toString());

        Map<String, Object> parameters = new HashMap<>();
        parameters.put("costctr", options.costCenterLabel());
        parameters.put("whcode", options.warehouseCode());
        parameters.put("repYear", options.reportYear());
        parameters.put("repMonth", options.reportMonth());

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

    private List<PhvObsoleteIdleRow> readRows(Path inputPath) throws IOException {
        return objectMapper.readValue(
                new String(Files.readAllBytes(inputPath), StandardCharsets.UTF_8),
                new TypeReference<List<PhvObsoleteIdleRow>>() {
                });
    }

    private String formatDate(String value) {
        if (value == null || value.trim().isEmpty()) {
            return value;
        }

        try {
            return OffsetDateTime.parse(value).format(DateTimeFormatter.ofPattern("dd/MM/yyyy"));
        } catch (Exception ignored) {
        }

        try {
            return LocalDateTime.parse(value).format(DateTimeFormatter.ofPattern("dd/MM/yyyy"));
        } catch (Exception ignored) {
        }

        return value;
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
        // 1. Prefer the CLI --template argument when it points to an existing file
        if (fallbackTemplatePath != null && !fallbackTemplatePath.trim().isEmpty()) {
            Path cliPath = Paths.get(fallbackTemplatePath);
            if (Files.exists(cliPath)) {
                return cliPath;
            }
        }

        // 2. Fall back to the path from pathConfig.properties
        String reportType = "obsolete";
        if (fallbackTemplatePath != null && fallbackTemplatePath.toLowerCase().contains("damage")) {
            reportType = "damage";
        }

        String configuredPath = new ReadPath().getPath(reportType);
        if (configuredPath != null && !configuredPath.trim().isEmpty()) {
            return Paths.get(configuredPath);
        }

        // 3. Last resort: return the CLI path even if it doesn't exist (will fail with a clear error)
        return Paths.get(fallbackTemplatePath);
    }
}