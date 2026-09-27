CREATE DATABASE IF NOT EXISTS finance CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE finance;
CREATE TABLE IF NOT EXISTS categories (
 id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
 kind VARCHAR(10) NOT NULL,
 name VARCHAR(150) NOT NULL,
 UNIQUE KEY uq_kind_name (kind,name)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS ledger_entries (
 id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
 kind VARCHAR(10) NOT NULL,
 date DATE NOT NULL,
 description VARCHAR(500) NOT NULL,
 category_id BIGINT UNSIGNED NOT NULL,
 amount DECIMAL(18,2) NOT NULL,
 tax_percent DECIMAL(6,2) NOT NULL DEFAULT 0,
 amount_after_tax DECIMAL(18,2) NOT NULL,
 currency_code CHAR(3) NOT NULL DEFAULT 'USD',
 order_code VARCHAR(150) NULL,
 sale_region VARCHAR(30) NULL,
 sales_channel VARCHAR(40) NULL,
 product_qty INT NULL,
 payee VARCHAR(255) NULL,
 origin_scope VARCHAR(30) NULL,
 payment_method VARCHAR(40) NULL,
 source VARCHAR(20) NULL,
 details JSON NULL,
 created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
 deleted_at DATETIME NULL,
 INDEX idx_kind_date (kind,date),
 CONSTRAINT fk_ledger_category FOREIGN KEY (category_id) REFERENCES categories(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS attachments (
 id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
 entry_id BIGINT UNSIGNED NOT NULL,
 original_name VARCHAR(255) NOT NULL,
 stored_name VARCHAR(100) NOT NULL,
 content_type VARCHAR(150) NOT NULL,
 size_bytes BIGINT NOT NULL,
 created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 INDEX idx_attachment_entry (entry_id),
 CONSTRAINT fk_attachment_entry FOREIGN KEY (entry_id) REFERENCES ledger_entries(id) ON DELETE CASCADE
) ENGINE=InnoDB;
INSERT IGNORE INTO categories (id,kind,name) VALUES
(1,'INCOME','Bán hàng'),(2,'INCOME','Thu khác'),
(3,'EXPENSE','Nguyên vật liệu'),(4,'EXPENSE','Bao bì / đóng gói'),
(5,'EXPENSE','Vận chuyển'),(6,'EXPENSE','Quảng cáo'),
(7,'EXPENSE','Phí dịch vụ'),(8,'EXPENSE','Lương nhân viên'),
(9,'EXPENSE','Điện / nước / Internet'),(10,'EXPENSE','Thuê mặt bằng'),
(11,'EXPENSE','Công cụ / thiết bị'),(12,'EXPENSE','Chi khác');
