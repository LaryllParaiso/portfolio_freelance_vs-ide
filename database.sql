-- Portfolio CMS Database Schema
-- Compatible with MySQL 5.7+ / MariaDB 10.2+ (XAMPP)

SET NAMES utf8mb4;
SET time_zone = '+00:00';

-- If you want the script to create a database automatically, uncomment and edit:
-- CREATE DATABASE IF NOT EXISTS portfolio_cms CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
-- USE portfolio_cms;

-- --------------------------------------------------------
-- Table: admins
-- Stores the single admin account (enforced at application layer).
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS admins (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  username VARCHAR(50) NOT NULL,
  email VARCHAR(255) NOT NULL,
  phone VARCHAR(30) NULL,
  location VARCHAR(120) NULL,
  password_hash VARCHAR(255) NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_admins_username (username),
  UNIQUE KEY uq_admins_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: admin_social_links
-- Stores admin "Connect with me" icon + URL links.
-- icon stores an uploaded image filename in /uploads.
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS admin_social_links (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  admin_id INT UNSIGNED NOT NULL,
  label VARCHAR(60) NULL,
  icon VARCHAR(255) NOT NULL,
  url VARCHAR(255) NOT NULL,
  display_order INT NOT NULL DEFAULT 0,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_admin_social_admin_order (admin_id, display_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: home_content
-- Stores hero content and highlights for the Home section.
-- highlights stores a JSON array of strings.
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS home_content (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  hero_title VARCHAR(255) NOT NULL,
  hero_subtitle VARCHAR(255) NOT NULL,
  hero_cta_text VARCHAR(80) NOT NULL,
  hero_cta_link VARCHAR(255) NOT NULL,
  highlights JSON NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_home_content_is_active (is_active),
  KEY idx_home_content_updated_at (updated_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: about_content
-- Stores About content including bio, profile image path, skills and experience.
-- skills stores a JSON array of strings.
-- experience stores a JSON array of objects.
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS about_content (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  bio TEXT NOT NULL,
  profile_image VARCHAR(255) NULL,
  skills JSON NULL,
  experience JSON NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_about_content_is_active (is_active),
  KEY idx_about_content_updated_at (updated_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: services
-- Stores service cards shown on the public site.
-- tags is stored as a comma-separated string (e.g., "Design,UI") for simplicity.
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS services (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  title VARCHAR(120) NOT NULL,
  description TEXT NOT NULL,
  pricing VARCHAR(100) NULL,
  tags VARCHAR(255) NULL,
  display_order INT NOT NULL DEFAULT 0,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_services_is_active_order (is_active, display_order),
  KEY idx_services_display_order (display_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: projects
-- Stores portfolio projects.
-- images stores a JSON array of image filenames.
-- tech_stack stores a JSON array of strings.
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS projects (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  title VARCHAR(160) NOT NULL,
  description TEXT NOT NULL,
  images JSON NULL,
  project_link VARCHAR(255) NULL,
  tech_stack JSON NULL,
  display_order INT NOT NULL DEFAULT 0,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_projects_is_active_order (is_active, display_order),
  KEY idx_projects_display_order (display_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: contact_messages
-- Stores contact form submissions.
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS contact_messages (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  name VARCHAR(120) NOT NULL,
  email VARCHAR(255) NOT NULL,
  message TEXT NOT NULL,
  is_read TINYINT(1) NOT NULL DEFAULT 0,
  submitted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  read_at TIMESTAMP NULL DEFAULT NULL,
  PRIMARY KEY (id),
  KEY idx_contact_messages_is_read (is_read),
  KEY idx_contact_messages_submitted_at (submitted_at),
  KEY idx_contact_messages_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: system_settings
-- Stores simple key/value configuration (e.g., 'setup_completed').
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS system_settings (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  setting_key VARCHAR(100) NOT NULL,
  setting_value TEXT NULL,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_system_settings_setting_key (setting_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
