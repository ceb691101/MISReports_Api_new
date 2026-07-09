package com.ceb.reporting.phvobsoleteidle.util;

import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Properties;

public class ReadPath {
    private static final String PROP_FILE = "pathConfig.properties";

    public String getPath() throws IOException {
        return getPath("obsolete");
    }

    public String getPath(String reportType) throws IOException {
        Properties properties = new Properties();

        Path externalPath = Paths.get(System.getProperty("user.home"), "Downloads", PROP_FILE);
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
        String rawPath;
        if (operatingSystem.contains("win")) {
            rawPath = properties.getProperty(key);
        } else {
            rawPath = properties.getProperty(key + "_LINUX");
        }

        return resolveDynamicPath(rawPath);
    }

    private String resolveDynamicPath(String path) {
        if (path == null) {
            return null;
        }

        String userHome = System.getProperty("user.home");
        userHome = userHome.replace("\\", "/");

        // Resolve ${user.home} placeholder if present
        if (path.contains("${user.home}")) {
            path = path.replace("${user.home}", userHome);
        }

        // Fallback: If path has a hardcoded C:/Users/Thamodi Walpola or C:/Users/<username>
        // and that path doesn't exist, dynamically replace C:/Users/<username> with the current userHome
        if (path.startsWith("C:/Users/") || path.startsWith("c:/Users/")) {
            Path p = Paths.get(path);
            if (!Files.exists(p)) {
                int desktopIdx = path.indexOf("/Desktop/");
                if (desktopIdx != -1) {
                    path = userHome + path.substring(desktopIdx);
                }
            }
        }

        return path;
    }
}