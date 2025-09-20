/**
 * Utility class for transforming object properties
 */
export class ObjectTransformer {
  /**
   * Converts all property names in an object or array to camelCase recursively
   * @param data The input object or array to transform
   * @returns The transformed data with camelCase property names
   */
  static toCamelCase(data: any): any {
    if (Array.isArray(data)) {
      return data.map(item => this.toCamelCase(item));
    }

    if (data !== null && typeof data === 'object') {
      const newObj: any = {};
      
      Object.keys(data).forEach(key => {
        const camelKey = this.toCamelCaseString(key);
        newObj[camelKey] = this.toCamelCase(data[key]);
      });
      
      return newObj;
    }
    
    return data;
  }

  /**
   * Converts a string to camelCase
   * @param str The input string to convert
   * @returns The camelCase version of the string
   */
  private static toCamelCaseString(str: string): string {
    return str.replace(/[-_.]([a-z])/gi, (_, letter) => letter.toUpperCase())
      // Handle first character
      .replace(/^[A-Z]/, letter => letter.toLowerCase());
  }
}