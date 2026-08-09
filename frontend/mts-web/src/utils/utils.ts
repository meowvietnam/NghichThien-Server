export class FormatUtil {
    static number(value: number | null | undefined): string {
        return (value ?? 0).toLocaleString('vi-VN')
    }
}