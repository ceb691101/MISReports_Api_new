package com.ceb.reporting.phvobsoleteidle.util;

import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Properties;

public class ReadPath {
    private static final String PROP_FILE = "pathConfig.properties";

    public String getPath() throws IOException {
        return getPath("obsolete");
    }

    public String getPath(String reportType) throws IOException {
        Properties properties = new Properties();

        Path externalPath = Path.of(System.getProperty("user.home"), "Downloads", PROP_FILE);
        if (Files.exists(externalPath)) {
            try (InputStream inputStream = Files.newInputStream(externalPath)) {
                properties.load(inputStream);
            }
        } else {
            try (InputStream inputStream = ReadPath.class.getClassLoader().getResourceAsStream(PROP_FILE)) {
                if (inputStream == null) {
                    throw new IOException("Unable to find " + PROP_FILE + " in Downloads or on the classpath.");
                }

                properties.load(inputStream);
            }
        }

        String key = "Path";
        if ("damage".equalsIgnoreCase(reportType)) {
            key = "Path_Damage";
        }

        String operatingSystem = System.getProperty("os.name", "").toLowerCase();
        if (operatingSystem.contains("win")) {
            return properties.getProperty(key);
        }

        return properties.getProperty(key + "_LINUX");
    }
}