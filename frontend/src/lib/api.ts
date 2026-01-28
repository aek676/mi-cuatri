/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

/** Represents the category types for calendar events from Blackboard. */
export enum CalendarCategory {
  Course = "Course",
  GradebookColumn = "GradebookColumn",
  Institution = "Institution",
  OfficeHours = "OfficeHours",
  Personal = "Personal",
}

/** Clean calendar event item mapped from Blackboard raw calendar entries. */
export interface CalendarItemDto {
  /**
   * Gets the calendar item identifier mapped from the Blackboard 'id' field.
   * @minLength 1
   */
  calendarid: string;
  /**
   * Gets the event title.
   * @minLength 1
   */
  title: string;
  /**
   * Gets the event start date/time in UTC.
   * @format date-time
   */
  start: string;
  /**
   * Gets the event end date/time in UTC.
   * @format date-time
   */
  end: string;
  /** Gets the physical or virtual location of the event. */
  location?: string | null;
  /** Represents the category types for calendar events from Blackboard. */
  category: CalendarCategory;
  /**
   * Gets the cleaned subject/course name extracted from the calendar name using regex. Empty for Institution/Personal categories.
   * @minLength 1
   */
  subject: string;
  /**
   * Gets the hexadecimal color code for visual representation.
   * @minLength 1
   */
  color: string;
  /** Gets the optional event description. */
  description?: string | null;
}

/** Data transfer object for creating a new product. */
export interface CreateProductDto {
  name?: string | null;
  /** @format double */
  price?: number;
  /** @format int32 */
  quantity?: number;
}

/** Data transfer object for login requests. */
export interface LoginRequestDto {
  /**
   * The username for authentication.
   * @minLength 1
   */
  username: string;
  /**
   * The password for authentication.
   * @minLength 1
   */
  password: string;
}

/** Data transfer object for login responses. */
export interface LoginResponseDto {
  /** Indicates whether the authentication was successful. */
  isSuccess?: boolean;
  /**
   * The response message describing the authentication result.
   * @minLength 1
   */
  message: string;
  /** The session cookie for authenticated requests. */
  sessionCookie?: string | null;
}

export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  /** @format int32 */
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: any;
}

/** Data transfer object for product information. */
export interface ProductDto {
  id?: string | null;
  name?: string | null;
  /** @format double */
  price?: number;
  /** @format int32 */
  quantity?: number;
}

/** Data transfer object for updating an existing product. */
export interface UpdateProductDto {
  name?: string | null;
  /** @format double */
  price?: number;
  /** @format int32 */
  quantity?: number;
}

/** Data transfer object containing detailed user information. */
export interface UserDetailDto {
  /** The user's given name. */
  given?: string | null;
  /** The user's family name. */
  family?: string | null;
  /** The user's email address. */
  email?: string | null;
  /** The URL to the user's avatar image. */
  avatar?: string | null;
}

/** Data transfer object for user response information. */
export interface UserResponseDto {
  /** Indicates whether the user retrieval was successful. */
  isSuccess?: boolean;
  /** The response message describing the operation result. */
  message?: string | null;
  /** Data transfer object containing detailed user information. */
  userData?: UserDetailDto;
}

export type QueryParamsType = Record<string | number, any>;
export type ResponseFormat = keyof Omit<Body, "body" | "bodyUsed">;

export interface FullRequestParams extends Omit<RequestInit, "body"> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseFormat;
  /** request body */
  body?: unknown;
  /** base url */
  baseUrl?: string;
  /** request cancellation token */
  cancelToken?: CancelToken;
}

export type RequestParams = Omit<
  FullRequestParams,
  "body" | "method" | "query" | "path"
>;

export interface ApiConfig<SecurityDataType = unknown> {
  baseUrl?: string;
  baseApiParams?: Omit<RequestParams, "baseUrl" | "cancelToken" | "signal">;
  securityWorker?: (
    securityData: SecurityDataType | null,
  ) => Promise<RequestParams | void> | RequestParams | void;
  customFetch?: typeof fetch;
}

export interface HttpResponse<D extends unknown, E extends unknown = unknown>
  extends Response {
  data: D;
  error: E;
}

type CancelToken = Symbol | string | number;

export enum ContentType {
  Json = "application/json",
  JsonApi = "application/vnd.api+json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

export class HttpClient<SecurityDataType = unknown> {
  public baseUrl: string = "";
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private abortControllers = new Map<CancelToken, AbortController>();
  private customFetch = (...fetchParams: Parameters<typeof fetch>) =>
    fetch(...fetchParams);

  private baseApiParams: RequestParams = {
    credentials: "same-origin",
    headers: {},
    redirect: "follow",
    referrerPolicy: "no-referrer",
  };

  constructor(apiConfig: ApiConfig<SecurityDataType> = {}) {
    Object.assign(this, apiConfig);
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected encodeQueryParam(key: string, value: any) {
    const encodedKey = encodeURIComponent(key);
    return `${encodedKey}=${encodeURIComponent(typeof value === "number" ? value : `${value}`)}`;
  }

  protected addQueryParam(query: QueryParamsType, key: string) {
    return this.encodeQueryParam(key, query[key]);
  }

  protected addArrayQueryParam(query: QueryParamsType, key: string) {
    const value = query[key];
    return value.map((v: any) => this.encodeQueryParam(key, v)).join("&");
  }

  protected toQueryString(rawQuery?: QueryParamsType): string {
    const query = rawQuery || {};
    const keys = Object.keys(query).filter(
      (key) => "undefined" !== typeof query[key],
    );
    return keys
      .map((key) =>
        Array.isArray(query[key])
          ? this.addArrayQueryParam(query, key)
          : this.addQueryParam(query, key),
      )
      .join("&");
  }

  protected addQueryParams(rawQuery?: QueryParamsType): string {
    const queryString = this.toQueryString(rawQuery);
    return queryString ? `?${queryString}` : "";
  }

  private contentFormatters: Record<ContentType, (input: any) => any> = {
    [ContentType.Json]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.JsonApi]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.Text]: (input: any) =>
      input !== null && typeof input !== "string"
        ? JSON.stringify(input)
        : input,
    [ContentType.FormData]: (input: any) => {
      if (input instanceof FormData) {
        return input;
      }

      return Object.keys(input || {}).reduce((formData, key) => {
        const property = input[key];
        formData.append(
          key,
          property instanceof Blob
            ? property
            : typeof property === "object" && property !== null
              ? JSON.stringify(property)
              : `${property}`,
        );
        return formData;
      }, new FormData());
    },
    [ContentType.UrlEncoded]: (input: any) => this.toQueryString(input),
  };

  protected mergeRequestParams(
    params1: RequestParams,
    params2?: RequestParams,
  ): RequestParams {
    return {
      ...this.baseApiParams,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...(this.baseApiParams.headers || {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    };
  }

  protected createAbortSignal = (
    cancelToken: CancelToken,
  ): AbortSignal | undefined => {
    if (this.abortControllers.has(cancelToken)) {
      const abortController = this.abortControllers.get(cancelToken);
      if (abortController) {
        return abortController.signal;
      }
      return void 0;
    }

    const abortController = new AbortController();
    this.abortControllers.set(cancelToken, abortController);
    return abortController.signal;
  };

  public abortRequest = (cancelToken: CancelToken) => {
    const abortController = this.abortControllers.get(cancelToken);

    if (abortController) {
      abortController.abort();
      this.abortControllers.delete(cancelToken);
    }
  };

  public request = async <T = any, E = any>({
    body,
    secure,
    path,
    type,
    query,
    format,
    baseUrl,
    cancelToken,
    ...params
  }: FullRequestParams): Promise<HttpResponse<T, E>> => {
    const secureParams =
      ((typeof secure === "boolean" ? secure : this.baseApiParams.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const queryString = query && this.toQueryString(query);
    const payloadFormatter = this.contentFormatters[type || ContentType.Json];
    const responseFormat = format || requestParams.format;

    return this.customFetch(
      `${baseUrl || this.baseUrl || ""}${path}${queryString ? `?${queryString}` : ""}`,
      {
        ...requestParams,
        headers: {
          ...(requestParams.headers || {}),
          ...(type && type !== ContentType.FormData
            ? { "Content-Type": type }
            : {}),
        },
        signal:
          (cancelToken
            ? this.createAbortSignal(cancelToken)
            : requestParams.signal) || null,
        body:
          typeof body === "undefined" || body === null
            ? null
            : payloadFormatter(body),
      },
    ).then(async (response) => {
      const r = response as HttpResponse<T, E>;
      r.data = null as unknown as T;
      r.error = null as unknown as E;

      const responseToParse = responseFormat ? response.clone() : response;
      const data = !responseFormat
        ? r
        : await responseToParse[responseFormat]()
            .then((data) => {
              if (r.ok) {
                r.data = data;
              } else {
                r.error = data;
              }
              return r;
            })
            .catch((e) => {
              r.error = e;
              return r;
            });

      if (cancelToken) {
        this.abortControllers.delete(cancelToken);
      }

      if (!response.ok) throw data;
      return data;
    });
  };
}

/**
 * @title backend
 * @version 1.0
 */
export class Api<
  SecurityDataType extends unknown,
> extends HttpClient<SecurityDataType> {
  api = {
    /**
     * No description
     *
     * @tags Auth
     * @name AuthLoginUalCreate
     * @summary Authenticates a user with the provided credentials.
     * @request POST:/api/Auth/login-ual
     */
    authLoginUalCreate: (data: LoginRequestDto, params: RequestParams = {}) =>
      this.request<LoginResponseDto, string | LoginResponseDto>({
        path: `/api/Auth/login-ual`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Auth
     * @name AuthMeList
     * @summary Retrieves the current authenticated user's information.
     * @request GET:/api/Auth/me
     */
    authMeList: (params: RequestParams = {}) =>
      this.request<UserResponseDto, any>({
        path: `/api/Auth/me`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Calendar
     * @name CalendarList
     * @summary Gets calendar items from Blackboard in a 16-week window starting at the first day of the month for the provided date.
     * @request GET:/api/Calendar
     */
    calendarList: (
      query?: {
        /**
         * Reference date used to calculate the window.
         * @format date-time
         */
        currentDate?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<CalendarItemDto[], ProblemDetails | void>({
        path: `/api/Calendar`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
 * No description
 *
 * @tags ImageProxy
 * @name ImageProxyList
 * @summary Proxies an image request from Blackboard and returns the image stream as a blob.
Accepts token only via X-Session-Cookie header
 * @request GET:/api/ImageProxy
 */
    imageProxyList: (
      query?: {
        /** The URL of the image to proxy. */
        imageUrl?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<string, ProblemDetails>({
        path: `/api/ImageProxy`,
        method: "GET",
        query: query,
        format: "blob",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Products
     * @name ProductsList
     * @summary Retrieves all products.
     * @request GET:/api/Products
     */
    productsList: (params: RequestParams = {}) =>
      this.request<ProductDto[], any>({
        path: `/api/Products`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Products
     * @name ProductsCreate
     * @summary Creates a new product.
     * @request POST:/api/Products
     */
    productsCreate: (data: CreateProductDto, params: RequestParams = {}) =>
      this.request<ProductDto, any>({
        path: `/api/Products`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Products
     * @name ProductsDetail
     * @summary Retrieves a product by its identifier.
     * @request GET:/api/Products/{id}
     */
    productsDetail: (id: string, params: RequestParams = {}) =>
      this.request<ProductDto, ProblemDetails>({
        path: `/api/Products/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Products
     * @name ProductsUpdate
     * @summary Updates an existing product.
     * @request PUT:/api/Products/{id}
     */
    productsUpdate: (
      id: string,
      data: UpdateProductDto,
      params: RequestParams = {},
    ) =>
      this.request<void, any>({
        path: `/api/Products/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Products
     * @name ProductsDelete
     * @summary Deletes a product by its identifier.
     * @request DELETE:/api/Products/{id}
     */
    productsDelete: (id: string, params: RequestParams = {}) =>
      this.request<void, any>({
        path: `/api/Products/${id}`,
        method: "DELETE",
        ...params,
      }),
  };
}
