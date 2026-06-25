# PAYROLL_BUSINESS_FLOW.md

## Purpose

Dokumen ini menjelaskan urutan flow bisnis aplikasi payroll berdasarkan source of truth di [`CONTEXT.md`](../CONTEXT.md).

Dokumen ini fokus pada:
- alur bisnis end-to-end
- peran user di setiap tahap
- perubahan status payroll run
- output utama yang dihasilkan sistem

---

## Ringkasan Flow Utama

Urutan flow bisnis payroll:

1. Setup master data karyawan
2. Buat payroll run per periode
3. Jalankan kalkulasi payroll
4. Review hasil kalkulasi
5. Approve atau reject payroll
6. Lock payroll
7. Generate output payroll
8. Jalankan disbursement
9. Download reports

---

## 1. Setup Master Data Karyawan

**Aktor utama:** HR / Admin

Pada tahap ini user menyiapkan seluruh data dasar yang dibutuhkan untuk payroll:
- data identitas karyawan
- NPWP
- status PTKP
- tanggal join / resign
- komponen gaji
- tunjangan
- potongan tertentu

Tujuan tahap ini adalah memastikan semua data payroll source sudah tersedia sebelum periode payroll diproses.

---

## 2. Buat Payroll Run Per Periode

**Aktor utama:** HR / Admin

User membuat payroll run untuk periode tertentu, misalnya:
- Juli 2025
- Agustus 2025

Saat payroll run dibuat:
- sistem validasi agar periode yang sama tidak duplikat
- payroll run masuk ke status awal: `Draft`

Status pada tahap ini:
- `Draft`

---

## 3. Kalkulasi Payroll

**Aktor utama:** Sistem / background job

Setelah payroll run dibuat, sistem menjalankan proses kalkulasi payroll.

Perhitungan mencakup:
- gaji pokok
- tunjangan
- lembur
- BPJS
- PPh 21
- potongan
- take home pay

Perubahan status:
- `Draft` → `Calculating`
- `Calculating` → `Calculated`

Hasil tahap ini adalah line items payroll per karyawan.

---

## 4. Review Hasil Kalkulasi

**Aktor utama:** HR / Finance / Admin

User membuka detail payroll run untuk memeriksa:
- line items per karyawan
- total gross / deduction / net
- timeline event
- indikasi anomali

Saat review dimulai:
- status berubah dari `Calculated` ke `UnderReview`

Status pada tahap ini:
- `UnderReview`

---

## 5. Approval atau Rejection

**Aktor utama:** Finance / Admin

Setelah review, payroll bisa:

### Approve
Jika hasil payroll dianggap benar:
- status berubah ke `Approved`

### Reject
Jika ada kesalahan:
- status kembali ke `Draft`
- payroll bisa diperbaiki lalu dihitung ulang

Perubahan status:
- `UnderReview` → `Approved`
- `UnderReview` → `Draft`

---

## 6. Lock Payroll

**Aktor utama:** HR / Admin

Jika payroll sudah di-approve, user dapat melakukan lock payroll.

Efek bisnis dari lock:
- payroll dianggap final
- payroll tidak boleh diubah lagi
- payslip generation dapat dijalankan

Perubahan status:
- `Approved` → `Locked`

Status pada tahap ini:
- `Locked`

---

## 7. Generate Output Payroll

**Aktor utama:** HR / Admin / Sistem

Dari payroll yang sudah siap, sistem dapat menghasilkan output seperti:
- PDF payslip
- Excel export payroll
- bank transfer file

Output ini digunakan untuk:
- distribusi slip gaji
- arsip payroll
- proses pembayaran ke bank
- reporting internal

---

## 8. Disbursement

**Aktor utama:** HR / Admin

Setelah payroll di-lock, user dapat menjalankan proses disbursement:

1. Download / generate bank file
2. Initiate disbursement
3. Lakukan transfer di luar sistem
4. Confirm disbursement setelah transfer selesai

Perubahan status:
- `Locked` → `Disbursed`

Catatan:
- status `Disbursed` hanya terjadi setelah transfer dikonfirmasi selesai

---

## 9. Reporting

**Aktor utama:** HR / Finance / Admin

User dapat mengakses hasil payroll untuk kebutuhan pelaporan dan operasional:
- export Excel payroll
- bank file
- payslip PDF
- laporan payroll per periode

Tahap ini membantu tim dalam:
- audit internal
- administrasi payroll
- rekonsiliasi pembayaran

---

## State Machine Payroll Run

Urutan status payroll run adalah:

- `Draft`
- `Calculating`
- `Calculated`
- `UnderReview`
- `Approved`
- `Locked`
- `Disbursed`

Jika payroll ditolak saat review:
- `UnderReview` → `Draft`

---

## Ringkasan Sederhana

Kalau disederhanakan, flow bisnis payroll adalah:

**siapkan data** → **buat payroll** → **hitung** → **review** → **approve** → **lock** → **bayarkan** → **konfirmasi selesai**

---

## Catatan

Dokumen ini menjelaskan target flow bisnis aplikasi.
Untuk status implementasi aktual dan gap yang masih ada, tetap gunakan [`CONTEXT.md`](../CONTEXT.md) sebagai source of truth.
